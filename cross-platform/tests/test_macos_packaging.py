"""Script-level regression tests for #338, with no .NET or macOS dependency.

Run from the repository root:
    python3 -m unittest discover -s cross-platform/tests -p 'test_macos_packaging.py' -v

The actual repository scripts are copied into an isolated layout. uname,
codesign and dotnet are stubbed; copying, plist generation, permissions and tar
use real tools. No real .NET publish, signing or network access is needed.
"""

import json
import os
from pathlib import Path, PurePosixPath
import plistlib
import shutil
import stat
import subprocess
import sys
import tarfile
import tempfile
import unittest
import zipfile


CROSS_PLATFORM = Path(__file__).resolve().parents[1]
VERSION = "7.4.338"


class MacOSPackagingTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="macos packaging tests ")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        # Preserve the helper's expected sibling MusicLyricApp/Resources layout.
        self.script_dir = self.root / "checkout with spaces" / "cross-platform"
        self.script_dir.mkdir(parents=True)
        source = CROSS_PLATFORM / "package-macos-app.sh"
        self.assertTrue(source.is_file(), f"Packaging helper not found: {source}")
        self.script = self.script_dir / source.name
        shutil.copy2(source, self.script)
        self.fallback_icon = self.script_dir / "MusicLyricApp/Resources/app-logo.icns"
        self.fallback_icon.parent.mkdir(parents=True)
        self.fallback_icon.write_bytes(b"fallback icon fixture\x00")

        self.publish = self.root / "published files with spaces"
        self.publish.mkdir()
        self.payload = {
            "MusicLyricApp": b"#!/bin/sh\nexit 0\n",
            "NLog.config": b"<nlog />\n",
            "libfixture.dylib": b"native library fixture\x00",
            ".hidden-runtime": b"hidden payload\x00",
            ".hidden directory/settings": b"nested hidden payload\n",
            "data directory/payload.bin": b"\x00\x01\xff",
            "Resources/app-logo.icns": b"published icon fixture\x00",
        }
        for name, content in self.payload.items():
            path = self.publish / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(content)
        # The helper must make the bundled apphost executable, not rely on input mode.
        (self.publish / "MusicLyricApp").chmod(0o644)
        self.output = self.root / "output directory with spaces"
        self.cwd = self.root / "unrelated working directory"
        self.cwd.mkdir()
        self.bin = self.root / "stub commands"
        self.bin.mkdir()
        self.codesign_log = self.root / "codesign.jsonl"
        self.verify_log = self.root / "verify.jsonl"
        self.write_stub("uname", '''import os
import sys
if sys.argv[1:] == ["-m"]:
    print(os.environ.get("TEST_UNAME_ARCH", "arm64"))
else:
    print(os.environ.get("TEST_UNAME_OS", "Darwin"))
''')
        self.write_stub("codesign", '''import json
import os
from pathlib import Path
import sys
args = sys.argv[1:]
with open(os.environ["TEST_CODESIGN_LOG"], "a", encoding="utf-8") as log:
    log.write(json.dumps(args) + "\\n")
phase = "verify" if "--verify" in args else "sign"
target = Path(args[-1])
is_archive_verify = phase == "verify" and target.parent.name == "verify"
if phase == "verify":
    # Capture evidence while staging still exists, not merely the command args.
    with open(os.environ["TEST_VERIFY_LOG"], "a", encoding="utf-8") as log:
        log.write(json.dumps({
            "target": str(target),
            "host": (target / "Contents/MacOS/MusicLyricApp").read_bytes().hex(),
            "config": (target / "Contents/MacOS/NLog.config").read_bytes().hex(),
            "config_is_symlink": (target / "Contents/MacOS/NLog.config").is_symlink(),
            "archive_exists": (target.parent.parent / (target.stem + ".tar.gz")).is_file(),
        }) + "\\n")
if (os.environ.get("TEST_CODESIGN_FAIL") == phase or
        (is_archive_verify and os.environ.get("TEST_CODESIGN_FAIL") == "archive-verify")):
    print("intentional codesign " + phase + " failure", file=sys.stderr)
    sys.exit(23)
''')
        self.env = os.environ.copy()
        self.env.update({
            "PATH": str(self.bin) + os.pathsep + os.defpath,
            "TEST_CODESIGN_LOG": str(self.codesign_log),
            "TEST_VERIFY_LOG": str(self.verify_log),
            "TEST_UNAME_OS": "Darwin",
            "TEST_UNAME_ARCH": "arm64",
            "TEST_CODESIGN_FAIL": "",
        })

    def write_stub(self, name, body):
        path = self.bin / name
        path.write_text(f"#!{sys.executable}\n" + body, encoding="utf-8")
        path.chmod(0o755)

    def run_helper(self, rid="osx-arm64", *, env=None, cwd=None, args=None):
        arguments = ([VERSION, rid, str(self.publish), str(self.output)]
                     if args is None else args)
        return subprocess.run(
            ["bash", str(self.script), *arguments],
            cwd=self.cwd if cwd is None else cwd,
            env=self.env if env is None else {**self.env, **env},
            capture_output=True,
            text=True,
            timeout=30,
            check=False,
        )

    def bundle_paths(self, rid="osx-arm64", version=VERSION):
        stem = f"MusicLyricApp-{version}-{rid}"
        return self.output / (stem + ".app"), self.output / (stem + ".tar.gz")

    def signing_calls(self):
        if not self.codesign_log.exists():
            return []
        return [json.loads(line) for line in
                self.codesign_log.read_text(encoding="utf-8").splitlines()]

    def assert_success(self, result):
        self.assertEqual(result.returncode, 0,
                         f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}")

    def assert_failed_without_artifacts(self, result, rid="osx-arm64"):
        self.assertNotEqual(result.returncode, 0,
                            f"Unexpected success:\n{result.stdout}\n{result.stderr}")
        bundle, archive = self.bundle_paths(rid)
        self.assertFalse(bundle.exists(), f"Failed packaging left {bundle}")
        self.assertFalse(archive.exists(), f"Failed packaging left {archive}")

    def assert_bundle(self, rid, version=VERSION):
        bundle, archive = self.bundle_paths(rid, version)
        self.assertTrue(bundle.is_dir(), f"Missing bundle: {bundle}")
        self.assertTrue(archive.is_file(), f"Missing archive: {archive}")
        for name, content in self.payload.items():
            with self.subTest(payload=name):
                self.assertEqual((bundle / "Contents/MacOS" / name).read_bytes(), content)
        macos = bundle / "Contents/MacOS"
        resources = bundle / "Contents/Resources"
        code_names = {"MusicLyricApp", "libfixture.dylib"}
        self.assertEqual({path.name for path in macos.iterdir() if not path.is_symlink()},
                         code_names)
        for name in {PurePosixPath(name).parts[0] for name in self.payload}:
            path = macos / name
            if name in code_names:
                self.assertTrue(path.is_file())
                self.assertFalse(path.is_symlink())
                continue
            self.assertTrue(path.is_symlink(), f"Resource left directly in MacOS: {path}")
            target = Path(os.readlink(path))
            self.assertFalse(target.is_absolute(), f"Absolute resource link: {path}")
            self.assertEqual(path.resolve(strict=True), (resources / name).resolve(strict=True))
            self.assertIn(resources.resolve(), path.resolve().parents)
        for name, content in self.payload.items():
            if PurePosixPath(name).parts[0] not in code_names:
                self.assertEqual((resources / name).read_bytes(), content)
        executable = bundle / "Contents/MacOS/MusicLyricApp"
        self.assertTrue(executable.stat().st_mode & stat.S_IXUSR)
        self.assertEqual(
            (bundle / "Contents/Resources/app-logo.icns").read_bytes(),
            self.payload["Resources/app-logo.icns"],
        )
        with (bundle / "Contents/Info.plist").open("rb") as stream:
            info = plistlib.load(stream)
        for key, value in {
            "CFBundleExecutable": "MusicLyricApp",
            "CFBundleVersion": version,
            "CFBundleShortVersionString": version,
            "CFBundlePackageType": "APPL",
            "CFBundleIconFile": "app-logo.icns",
        }.items():
            self.assertEqual(info.get(key), value, key)

        # Inspect rather than extract: all members must live under the named .app.
        with tarfile.open(archive, "r:gz") as tar:
            members = {}
            for member in tar.getmembers():
                path = PurePosixPath(member.name)
                self.assertFalse(path.is_absolute(), member.name)
                self.assertNotIn("..", path.parts, member.name)
                self.assertTrue(path.parts, member.name)
                self.assertEqual(path.parts[0], bundle.name, member.name)
                members[str(path)] = member
            for path in bundle.rglob("*"):
                relative = str(PurePosixPath(bundle.name) / path.relative_to(bundle).as_posix())
                self.assertIn(relative, members)
                if path.is_symlink():
                    self.assertTrue(members[relative].issym(), relative)
                    self.assertEqual(members[relative].linkname, os.readlink(path))
                elif path.is_file():
                    self.assertTrue(members[relative].isfile(), relative)
                    archived = tar.extractfile(members[relative])
                    self.assertIsNotNone(archived, relative)
                    with archived:
                        self.assertEqual(archived.read(), path.read_bytes(), relative)
            apphost = members[f"{bundle.name}/Contents/MacOS/MusicLyricApp"]
            self.assertTrue(apphost.mode & stat.S_IXUSR)

        calls = [args for args in self.signing_calls() if Path(args[-1]).name == bundle.name]
        self.assertEqual(len(calls), 3, calls)
        self.assertTrue("--sign" in calls[0] or "-s" in calls[0])
        self.assertIn("--verify", calls[1])
        self.assertIn("--verify", calls[2])
        staged = Path(calls[0][-1])
        self.assertEqual(Path(calls[1][-1]), staged)
        self.assertEqual(Path(calls[2][-1]), staged.parent / "verify" / bundle.name)
        snapshots = [json.loads(line) for line in
                     self.verify_log.read_text(encoding="utf-8").splitlines()]
        snapshots = [record for record in snapshots if Path(record["target"]).name == bundle.name]
        self.assertEqual(len(snapshots), 2)
        for record in snapshots:
            self.assertEqual(bytes.fromhex(record["host"]), self.payload["MusicLyricApp"])
            self.assertEqual(bytes.fromhex(record["config"]), self.payload["NLog.config"])
            self.assertTrue(record["config_is_symlink"])
        self.assertTrue(snapshots[1]["archive_exists"],
                        "Extracted-bundle verification ran without the staged archive")

    def test_both_architectures_bundle_plist_archive_and_signing(self):
        for rid in ("osx-arm64", "osx-x64"):
            with self.subTest(rid=rid):
                before = len(self.signing_calls())
                self.assert_success(self.run_helper(rid))
                self.assert_bundle(rid)
                calls = self.signing_calls()[before:]
                sign = [args for args in calls if "--sign" in args or "-s" in args]
                verify = [args for args in calls if "--verify" in args]
                self.assertTrue(sign, f"No signing call: {calls}")
                self.assertTrue(verify, f"No verification call: {calls}")
                self.assertLess(calls.index(sign[0]), calls.index(verify[0]))
                # Packaging must not change the source executable or payloads.
                for name, content in self.payload.items():
                    self.assertEqual((self.publish / name).read_bytes(), content)
                self.assertFalse((self.publish / "MusicLyricApp").stat().st_mode & stat.S_IXUSR)

    def test_fallback_icon_is_resolved_relative_to_script_not_cwd(self):
        (self.publish / "Resources/app-logo.icns").unlink()
        self.assert_success(self.run_helper())
        bundle, _ = self.bundle_paths()
        self.assertEqual((bundle / "Contents/Resources/app-logo.icns").read_bytes(),
                         self.fallback_icon.read_bytes())

    def test_publish_icon_does_not_require_fallback_icon(self):
        self.fallback_icon.unlink()
        self.assert_success(self.run_helper())
        self.assert_bundle("osx-arm64")

    def test_relative_input_and_output_from_different_cwd(self):
        self.assert_success(self.run_helper(args=[
            VERSION, "osx-arm64", os.path.relpath(self.publish, self.cwd),
            os.path.relpath(self.output, self.cwd),
        ]))
        self.assert_bundle("osx-arm64")

    def test_missing_required_publish_files(self):
        for name in ("MusicLyricApp", "NLog.config"):
            with self.subTest(missing=name):
                path = self.publish / name
                path.unlink()
                try:
                    self.assert_failed_without_artifacts(self.run_helper())
                    self.assertEqual(self.signing_calls(), [])
                finally:
                    path.write_bytes(self.payload[name])
                    path.chmod(0o644)

    def test_missing_publish_directory(self):
        shutil.rmtree(self.publish)
        self.assert_failed_without_artifacts(self.run_helper())
        self.assertEqual(self.signing_calls(), [])

    def test_missing_both_icon_sources(self):
        (self.publish / "Resources/app-logo.icns").unlink()
        self.fallback_icon.unlink()
        self.assert_failed_without_artifacts(self.run_helper())

    def test_signing_failure_leaves_no_final_artifacts(self):
        result = self.run_helper(env={"TEST_CODESIGN_FAIL": "sign"})
        self.assert_failed_without_artifacts(result)
        calls = self.signing_calls()
        self.assertTrue(calls, "Signing failure stub was never called")
        self.assertFalse(any("--verify" in args for args in calls))

    def test_verification_failure_leaves_no_final_artifacts(self):
        result = self.run_helper(env={"TEST_CODESIGN_FAIL": "verify"})
        self.assert_failed_without_artifacts(result)
        self.assertTrue(any("--verify" in args for args in self.signing_calls()))

    def test_archive_verification_failure_leaves_no_final_artifacts(self):
        result = self.run_helper(env={"TEST_CODESIGN_FAIL": "archive-verify"})
        self.assert_failed_without_artifacts(result)
        calls = self.signing_calls()
        verifies = [args for args in calls if "--verify" in args]
        self.assertEqual(len(verifies), 2)
        self.assertEqual(Path(verifies[-1][-1]).parent.name, "verify")

    def test_rejects_output_equal_to_or_inside_publish_directory(self):
        for output in (self.publish, self.publish / "nested output" / "packages"):
            with self.subTest(output=output):
                self.output = output
                self.assert_failed_without_artifacts(self.run_helper())
                self.assertEqual(self.signing_calls(), [])
                self.assertFalse(list(self.publish.rglob(".macos-package.*")))
                for name, content in self.payload.items():
                    self.assertEqual((self.publish / name).read_bytes(), content)

    def test_rejects_non_darwin_host(self):
        self.assert_failed_without_artifacts(
            self.run_helper(env={"TEST_UNAME_OS": "Linux"}))
        self.assertEqual(self.signing_calls(), [])

    def test_rejects_unsupported_rids(self):
        for rid in ("linux-x64", "win-x64", "osx-arm", "osx-universal"):
            with self.subTest(rid=rid):
                self.assert_failed_without_artifacts(self.run_helper(rid), rid)
                self.assertEqual(self.signing_calls(), [])

    def test_missing_cli_arguments_fail_without_artifacts(self):
        for args in ([], [VERSION], [VERSION, "osx-arm64"],
                     [VERSION, "osx-arm64", str(self.publish)]):
            with self.subTest(args=args):
                self.assert_failed_without_artifacts(self.run_helper(args=args))
                self.assertEqual(self.signing_calls(), [])

    def prepare_entrypoints(self):
        for name in ("app.sh", "build-macos-app.sh", "publish.sh"):
            shutil.copy2(CROSS_PLATFORM / name, self.script_dir / name)
        for project in ("MusicLyricApp", "MusicLyricApp.Tests"):
            assets = self.script_dir / project / "obj/project.assets.json"
            assets.parent.mkdir(parents=True, exist_ok=True)
            assets.write_text("{}", encoding="utf-8")
        self.project_file = self.script_dir / "MusicLyricApp/MusicLyricApp.csproj"
        self.write_project_version(VERSION)
        self.env.pop("APP_VERSION", None)
        self.env.pop("TEST_PROJECT_VERSION", None)
        self.output = self.script_dir / "publish"
        self.output.mkdir()
        self.payload["MusicLyricApp"] = (
            b"#!/bin/sh\necho BUNDLE_HOST_MARKER\npwd -P\n"
        )
        self.dotnet_log = self.root / "dotnet.jsonl"
        self.env.update({
            "TEST_PUBLISH_FAIL_RID": "",
            "TEST_DOTNET_LOG": str(self.dotnet_log),
            "TEST_PUBLISH_PAYLOAD": json.dumps({
                name: content.hex() for name, content in self.payload.items()
            }),
        })
        self.write_stub("dotnet", '''import json
import os
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
args = sys.argv[1:]
with open(os.environ["TEST_DOTNET_LOG"], "a", encoding="utf-8") as log:
    log.write(json.dumps(args) + "\\n")
if args == ["--list-sdks"]:
    if os.environ.get("TEST_NO_SDK") == "1":
        sys.exit(92)
    print("9.0.100 [/stub/dotnet/sdk]")
elif (len(args) == 5 and args[0] == "msbuild" and
      args[2:] == ["-nologo", "-verbosity:quiet", "-getProperty:Version"]):
    version = os.environ.get("TEST_PROJECT_VERSION")
    if version is None:
        version = ET.parse(args[1]).findtext("PropertyGroup/Version", default="")
    print(version)
    if os.environ.get("TEST_VERSION_QUERY_FAIL") == "1":
        print("intentional version query failure", file=sys.stderr)
        sys.exit(94)
elif args and args[0] in ("build", "run"):
    print("DOTNET_" + args[0].upper() + "_STUB")
elif args and args[0] == "publish":
    rid = args[args.index("-r") + 1]
    if os.environ.get("TEST_PUBLISH_FAIL_RID") == rid:
        print("intentional publish failure for " + rid, file=sys.stderr)
        sys.exit(93)
    output = Path(args[args.index("-o") + 1])
    host_name = "MusicLyricApp.exe" if rid.startswith("win-") else "MusicLyricApp"
    for name, content in json.loads(os.environ["TEST_PUBLISH_PAYLOAD"]).items():
        if name == "MusicLyricApp":
            name = host_name
        path = output / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(bytes.fromhex(content))
    (output / host_name).chmod(0o755)
else:
    print("Unexpected dotnet call: " + repr(args), file=sys.stderr)
    sys.exit(91)
''')

    def write_project_version(self, version):
        from xml.sax.saxutils import escape
        self.project_file.write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            f'<Version>{escape(version)}</Version></PropertyGroup></Project>',
            encoding="utf-8",
        )

    def version_query(self):
        return ["msbuild", str(self.project_file), "-nologo", "-verbosity:quiet",
                "-getProperty:Version"]

    def run_entrypoint(self, name, *args, env=None, shell="bash"):
        return subprocess.run(
            [shell, str(self.script_dir / name), *args],
            cwd=self.cwd,
            env={**self.env, **(env or {})},
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=30,
            check=False,
        )

    def dotnet_calls(self):
        if not self.dotnet_log.exists():
            return []
        return [json.loads(line) for line in
                self.dotnet_log.read_text(encoding="utf-8").splitlines()]

    def create_mid_archive(self, rid):
        archive = self.output / f"MusicLyricApp-{VERSION}-{rid}-mid.tar.gz"
        with tarfile.open(archive, "w:gz") as tar:
            tar.add(self.publish, arcname=".")
        return archive

    def test_app_package_uses_helper_for_detected_and_explicit_macos_rids(self):
        self.prepare_entrypoints()
        for rid, arguments in (("osx-arm64", ()), ("osx-x64", ("osx-x64",))):
            with self.subTest(rid=rid):
                self.assert_success(self.run_entrypoint("app.sh", "package", *arguments))
                self.assert_bundle(rid)
                self.assertFalse((self.cwd / "publish").exists())
        calls = self.dotnet_calls()
        publishes = [args for args in calls if args[0] == "publish"]
        self.assertEqual(len(publishes), 2)
        self.assertEqual(sum(args == ["--list-sdks"] for args in calls), 2)
        self.assertEqual(sum(args == self.version_query() for args in calls), 2)
        self.assertEqual(len(calls), 6, f"Unexpected restore/build calls: {calls}")
        for args, rid in zip(publishes, ("osx-arm64", "osx-x64")):
            self.assertEqual(args[1], str(self.script_dir / "MusicLyricApp/MusicLyricApp.csproj"))
            self.assertEqual(args[args.index("-r") + 1], rid)
            self.assertEqual(args[args.index("-o") + 1],
                             str(self.output / VERSION / rid))
        signing = self.signing_calls()
        self.assertEqual(sum("--verify" in args for args in signing), 4)
        self.assertEqual(sum("--sign" in args or "-s" in args for args in signing), 2)

    def test_app_run_package_launches_bundle_not_raw_publish_directory(self):
        self.prepare_entrypoints()
        for rid, arch, arguments in (
            ("osx-arm64", "arm64", ()),
            ("osx-x64", "x86_64", ("osx-x64",)),
        ):
            with self.subTest(rid=rid):
                self.assert_success(self.run_entrypoint("app.sh", "package", rid))
                raw_host = self.output / VERSION / rid / "MusicLyricApp"
                raw_host.write_text("#!/bin/sh\necho RAW_HOST_MARKER\nexit 77\n",
                                    encoding="utf-8")
                raw_host.chmod(0o755)
                before = len(self.dotnet_calls())
                result = self.run_entrypoint(
                    "app.sh", "run-package", *arguments,
                    env={"TEST_UNAME_ARCH": arch},
                )
                self.assert_success(result)
                bundle, _ = self.bundle_paths(rid)
                self.assertIn("BUNDLE_HOST_MARKER", result.stdout.splitlines())
                self.assertIn(str((bundle / "Contents/MacOS").resolve()),
                              result.stdout.splitlines())
                self.assertNotIn("RAW_HOST_MARKER", result.stdout)
                self.assertEqual(self.dotnet_calls()[before:],
                                 [["--list-sdks"], self.version_query()])

    def test_app_run_package_does_not_fall_back_to_raw_host_when_bundle_missing(self):
        self.prepare_entrypoints()
        raw_host = self.output / VERSION / "osx-arm64/MusicLyricApp"
        raw_host.parent.mkdir(parents=True)
        raw_host.write_text("#!/bin/sh\necho RAW_HOST_MARKER\n", encoding="utf-8")
        raw_host.chmod(0o755)
        result = self.run_entrypoint("app.sh", "run-package", "osx-arm64")
        self.assertNotEqual(result.returncode, 0)
        self.assertNotIn("RAW_HOST_MARKER", result.stdout)
        self.assertEqual(self.dotnet_calls(), [["--list-sdks"], self.version_query()])

    def test_app_package_version_changes_with_project_or_msbuild_result(self):
        self.prepare_entrypoints()
        for version, source in (("8.2.41", "project"), ("9.1.52", "stub")):
            with self.subTest(source=source, version=version):
                env = {}
                if source == "project":
                    self.write_project_version(version)
                else:
                    env["TEST_PROJECT_VERSION"] = version
                self.assert_success(self.run_entrypoint("app.sh", "package", "osx-arm64", env=env))
                self.assert_bundle("osx-arm64", version)
                self.assertTrue((self.output / version / "osx-arm64/MusicLyricApp").is_file())
                for path in self.bundle_paths():
                    self.assertFalse(path.exists(), "Default version leaked into artifact name")
        self.assertEqual(sum(args == self.version_query() for args in self.dotnet_calls()), 2)

    def test_app_version_environment_does_not_override_project_version(self):
        self.prepare_entrypoints()
        self.assert_success(self.run_entrypoint(
            "app.sh", "package", "osx-arm64", env={"APP_VERSION": "99.99.99"}))
        self.assert_bundle("osx-arm64")
        for path in self.bundle_paths(version="99.99.99"):
            self.assertFalse(path.exists())
        self.assertFalse((self.output / "99.99.99").exists())
        self.assertIn(self.version_query(), self.dotnet_calls())

    def test_app_invalid_or_empty_project_versions_prevent_publish(self):
        self.prepare_entrypoints()
        for command in ("package", "run-package", "all", "dist"):
            for version in ("", "bad version", "../escape", "7.4/338"):
                with self.subTest(command=command, version=version):
                    before = len(self.dotnet_calls())
                    result = self.run_entrypoint("app.sh", command,
                                                 env={"TEST_PROJECT_VERSION": version})
                    self.assertNotEqual(result.returncode, 0)
                    calls = self.dotnet_calls()[before:]
                    self.assertIn(self.version_query(), calls)
                    self.assertFalse(any(args[0] in ("publish", "run", "build") for args in calls))
                    self.assertEqual(list(self.output.iterdir()), [])
                    self.assertEqual(self.signing_calls(), [])

    def test_app_dist_packages_five_release_artifacts_in_order_without_running_host(self):
        self.prepare_entrypoints()
        result = self.run_entrypoint("app.sh", "dist")
        self.assert_success(result)
        rids = ("win-x86", "win-x64", "linux-x64", "osx-x64", "osx-arm64")
        calls = self.dotnet_calls()
        publishes = [args for args in calls if args[0] == "publish"]
        self.assertEqual([args[args.index("-r") + 1] for args in publishes], list(rids))
        self.assertEqual(calls, [["--list-sdks"], self.version_query(), *publishes])
        archives = []
        for rid, args in zip(rids, publishes):
            with self.subTest(rid=rid):
                self.assertEqual(args[1], str(self.project_file))
                self.assertEqual(args[args.index("-o") + 1],
                                 str(self.output / VERSION / rid))
                suffix = ".zip" if rid.startswith("win-") else ".tar.gz"
                archive = self.output / f"MusicLyricApp-{VERSION}-{rid}{suffix}"
                archives.append(archive)
                self.assertTrue(archive.is_file(), str(archive))
                self.assertIn(str(archive), result.stdout)
                if rid.startswith("win-"):
                    self.assertTrue(zipfile.is_zipfile(archive))
                    with zipfile.ZipFile(archive) as zipped:
                        members = {str(PurePosixPath(name)): name for name in zipped.namelist()}
                        self.assertNotIn("MusicLyricApp", members)
                        for name, content in (("MusicLyricApp.exe", self.payload["MusicLyricApp"]),
                                              ("NLog.config", self.payload["NLog.config"])):
                            self.assertIn(name, members)
                            self.assertEqual(zipped.read(members[name]), content)
                    raw = self.output / VERSION / rid
                    self.assertTrue((raw / "MusicLyricApp.exe").is_file())
                    self.assertFalse((raw / "MusicLyricApp").exists())
                elif rid == "linux-x64":
                    with tarfile.open(archive, "r:gz") as tar:
                        members = {str(PurePosixPath(member.name)): member
                                   for member in tar.getmembers()}
                        self.assertNotIn("MusicLyricApp.exe", members)
                        for name in ("MusicLyricApp", "NLog.config"):
                            self.assertIn(name, members)
                            self.assertTrue(members[name].isfile())
                            with tar.extractfile(members[name]) as stream:
                                self.assertEqual(stream.read(), self.payload[name])
                        self.assertTrue(members["MusicLyricApp"].mode & stat.S_IXUSR)
                else:
                    self.assert_bundle(rid)
        self.assertEqual(set(self.output.glob("*.zip")) | set(self.output.glob("*.tar.gz")),
                         set(archives))
        self.assertIn(f"全部 5 个分发包已生成：{self.output}", result.stdout)
        self.assertNotIn("BUNDLE_HOST_MARKER", result.stdout)
        self.assertEqual([Path(args[-1]).name for args in self.signing_calls()
                          if "--sign" in args or "-s" in args],
                         [self.bundle_paths(rid)[0].name for rid in ("osx-x64", "osx-arm64")])

    def test_app_dist_cleans_entire_publish_directory_before_first_publish(self):
        self.prepare_entrypoints()
        stale_paths = (
            self.output / "old-version/stale.app/Contents/old-file",
            self.output / VERSION / "win-x86/stale-file",
            self.output / f"MusicLyricApp-{VERSION}-win-x86.zip",
            self.output / ".hidden-stale-file",
        )
        for path in stale_paths:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("old artifact", encoding="utf-8")
        outside = self.cwd / "keep.txt"
        outside.write_text("keep", encoding="utf-8")

        # Even when the first publish fails, no artifacts from the previous run may remain.
        result = self.run_entrypoint("app.sh", "dist",
                                     env={"TEST_PUBLISH_FAIL_RID": "win-x86"})
        self.assertNotEqual(result.returncode, 0)
        self.assertTrue(self.output.is_dir())
        self.assertEqual(list(self.output.iterdir()), [])
        self.assertEqual(outside.read_text(encoding="utf-8"), "keep")
        publishes = [args for args in self.dotnet_calls() if args[0] == "publish"]
        self.assertEqual([args[args.index("-r") + 1] for args in publishes], ["win-x86"])

    def test_app_dist_invalid_invocations_preserve_existing_artifacts(self):
        self.prepare_entrypoints()
        marker = self.output / "keep.zip"
        marker.write_bytes(b"existing artifact")
        for arguments, env in (
            ((), {"TEST_UNAME_OS": "Linux"}),
            (("osx-arm64",), {}),
            ((), {"TEST_PROJECT_VERSION": ""}),
            ((), {"TEST_NO_SDK": "1"}),
        ):
            with self.subTest(arguments=arguments, env=env):
                result = self.run_entrypoint("app.sh", "dist", *arguments, env=env)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(marker.read_bytes(), b"existing artifact")

    def test_app_package_preserves_other_publish_artifacts(self):
        self.prepare_entrypoints()
        marker = self.output / "keep.zip"
        marker.write_bytes(b"existing artifact")
        self.assert_success(self.run_entrypoint("app.sh", "package", "osx-arm64"))
        self.assertEqual(marker.read_bytes(), b"existing artifact")
        self.assert_bundle("osx-arm64")

    def test_app_dist_publish_failure_stops_remaining_targets_without_success_summary(self):
        self.prepare_entrypoints()
        result = self.run_entrypoint("app.sh", "dist",
                                     env={"TEST_PUBLISH_FAIL_RID": "linux-x64"})
        self.assertNotEqual(result.returncode, 0)
        calls = self.dotnet_calls()
        publishes = [args for args in calls if args[0] == "publish"]
        self.assertEqual([args[args.index("-r") + 1] for args in publishes],
                         ["win-x86", "win-x64", "linux-x64"])
        self.assertEqual(calls, [["--list-sdks"], self.version_query(), *publishes])
        for rid in ("win-x86", "win-x64"):
            self.assertTrue((self.output / f"MusicLyricApp-{VERSION}-{rid}.zip").is_file())
        self.assertFalse((self.output / f"MusicLyricApp-{VERSION}-linux-x64.tar.gz").exists())
        for rid in ("osx-x64", "osx-arm64"):
            self.assert_failed_without_artifacts(result, rid)
            self.assertFalse((self.output / VERSION / rid).exists())
        self.assertEqual(self.signing_calls(), [])
        self.assertNotIn("BUNDLE_HOST_MARKER", result.stdout)
        self.assertNotIn("全部 5 个分发包已生成", result.stdout + result.stderr)

    def test_app_dist_rejects_non_darwin_and_rid_arguments_without_publish(self):
        self.prepare_entrypoints()
        for arguments, env in (
            ((), {"TEST_UNAME_OS": "Linux"}),
            (("osx-arm64",), {}),
            (("osx-x64",), {}),
            (("linux-x64",), {}),
        ):
            with self.subTest(arguments=arguments, env=env):
                before = len(self.dotnet_calls())
                result = self.run_entrypoint("app.sh", "dist", *arguments, env=env)
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse(any(args[0] in ("publish", "run", "build")
                                     for args in self.dotnet_calls()[before:]))
                self.assertEqual(list(self.output.iterdir()), [])
                self.assertEqual(self.signing_calls(), [])

    def test_app_help_via_bash_and_sh_needs_no_sdk_or_version_query(self):
        self.prepare_entrypoints()
        for shell in ("bash", "sh"):
            for command in ("help", "--help", "-h"):
                with self.subTest(shell=shell, command=command):
                    result = self.run_entrypoint(
                        "app.sh", command, shell=shell,
                        env={"TEST_NO_SDK": "1", "TEST_PROJECT_VERSION": "", "BASH_VERSION": ""},
                    )
                    self.assert_success(result)
                    self.assertIn("dist", result.stdout)
                    self.assertIn("package", result.stdout)
                    self.assertEqual(self.dotnet_calls(), [])

    def test_app_run_and_build_do_not_query_project_version(self):
        self.prepare_entrypoints()
        for command in ("run", "build"):
            with self.subTest(command=command):
                before = len(self.dotnet_calls())
                self.assert_success(self.run_entrypoint(
                    "app.sh", command, env={"TEST_PROJECT_VERSION": ""}))
                calls = self.dotnet_calls()[before:]
                self.assertEqual(len(calls), 2)
                self.assertEqual(calls[0], ["--list-sdks"])
                self.assertEqual(calls[1][0], command)
                self.assertEqual(self.signing_calls(), [])

    def test_app_all_queries_project_version_then_builds_packages_and_runs_bundle(self):
        self.prepare_entrypoints()
        result = self.run_entrypoint("app.sh", "all", "osx-arm64")
        self.assert_success(result)
        calls = self.dotnet_calls()
        self.assertEqual(calls[:2], [["--list-sdks"], self.version_query()])
        self.assertEqual([args[0] for args in calls[2:]], ["build", "publish"])
        self.assert_bundle("osx-arm64")
        self.assertIn("BUNDLE_HOST_MARKER", result.stdout.splitlines())
        bundle, _ = self.bundle_paths()
        self.assertIn(str((bundle / "Contents/MacOS").resolve()), result.stdout.splitlines())

    def snapshot_tree(self, root):
        """Include empty directories, link targets and modes, without following links."""
        snapshot = {}
        for path in root.rglob("*"):
            mode = stat.S_IMODE(path.lstat().st_mode)
            if path.is_symlink():
                value = ("link", os.readlink(path), mode)
            elif path.is_dir():
                value = ("directory", mode)
            else:
                value = ("file", path.read_bytes(), mode)
            snapshot[path.relative_to(root).as_posix()] = value
        return snapshot

    def test_app_dist_via_sh_packages_five_targets_with_evaluated_version(self):
        self.prepare_entrypoints()
        version = "10.8.338-preview.2"
        result = self.run_entrypoint(
            "app.sh", "dist", shell="sh",
            env={"BASH_VERSION": "", "TEST_PROJECT_VERSION": version + "\r",
                 "APP_VERSION": "99.99.99"},
        )
        self.assert_success(result)
        publishes = [args for args in self.dotnet_calls() if args[0] == "publish"]
        rids = ("win-x86", "win-x64", "linux-x64", "osx-x64", "osx-arm64")
        self.assertEqual([args[args.index("-r") + 1] for args in publishes], list(rids))
        self.assertEqual(self.dotnet_calls(),
                         [["--list-sdks"], self.version_query(), *publishes])
        for args, rid in zip(publishes, rids):
            self.assertEqual(args[args.index("-o") + 1], str(self.output / version / rid))
            suffix = ".zip" if rid.startswith("win-") else ".tar.gz"
            self.assertTrue((self.output / f"MusicLyricApp-{version}-{rid}{suffix}").is_file())
        for rid in ("osx-x64", "osx-arm64"):
            self.assert_bundle(rid, version)
        self.assertFalse((self.output / VERSION).exists())
        self.assertFalse(any("99.99.99" in path.name for path in self.output.iterdir()))
        self.assertIn("全部 5 个分发包已生成", result.stdout)
        self.assertNotIn("BUNDLE_HOST_MARKER", result.stdout)

    def test_two_successful_dist_runs_remove_old_zip_members_and_directories(self):
        self.prepare_entrypoints()
        old_name = "retired directory/obsolete.bin"
        first_payload = {**self.payload, old_name: b"first release only"}
        self.assert_success(self.run_entrypoint("app.sh", "dist", env={
            "TEST_PUBLISH_PAYLOAD": json.dumps({
                name: content.hex() for name, content in first_payload.items()
            }),
        }))
        zip_path = self.output / f"MusicLyricApp-{VERSION}-win-x86.zip"
        with zipfile.ZipFile(zip_path) as archive:
            self.assertIn(old_name, archive.namelist())
        old_directory = self.output / "obsolete-version/empty directory"
        old_directory.mkdir(parents=True)
        hidden = self.output / ".retired-release"
        hidden.write_bytes(b"obsolete")
        project_marker = self.project_file.parent / "do-not-clean.txt"
        project_marker.write_bytes(b"project source")
        project_before = self.snapshot_tree(self.project_file.parent)
        outside_publish = self.cwd / "publish"
        outside_publish.mkdir()
        (outside_publish / "keep.zip").write_bytes(b"unrelated publish")

        self.assert_success(self.run_entrypoint("app.sh", "dist"))
        self.assertFalse(old_directory.parent.exists())
        self.assertFalse(hidden.exists())
        self.assertFalse(any(path.name == "obsolete.bin" for path in self.output.rglob("*")))
        self.assertFalse(any(path.name == "retired directory" for path in self.output.rglob("*")))
        for archive_path in self.output.glob("*.zip"):
            with zipfile.ZipFile(archive_path) as archive:
                self.assertFalse(any("retired directory" in name for name in archive.namelist()))
                self.assertEqual(archive.read("NLog.config"), self.payload["NLog.config"])
        for archive_path in self.output.glob("*.tar.gz"):
            with tarfile.open(archive_path) as archive:
                self.assertFalse(any("retired directory" in name for name in archive.getnames()))
        self.assertEqual(self.snapshot_tree(self.project_file.parent), project_before)
        self.assertEqual((outside_publish / "keep.zip").read_bytes(), b"unrelated publish")
        self.assertEqual(len([args for args in self.dotnet_calls() if args[0] == "publish"]), 10)

    def test_dist_invalid_versions_and_arguments_preserve_complete_publish_tree(self):
        self.prepare_entrypoints()
        self.assert_success(self.run_entrypoint("app.sh", "package", "osx-arm64"))
        (self.output / ".hidden/empty directory").mkdir(parents=True)
        before_tree = self.snapshot_tree(self.output)
        for arguments, env in (
            (("osx-x64",), {}),
            ((), {"TEST_PROJECT_VERSION": "bad version"}),
            ((), {"TEST_PROJECT_VERSION": "../escape"}),
            ((), {"TEST_PROJECT_VERSION": "7.4/338"}),
        ):
            with self.subTest(arguments=arguments, env=env):
                before_calls = len(self.dotnet_calls())
                before_signing = self.signing_calls()
                result = self.run_entrypoint("app.sh", "dist", *arguments, env=env)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(self.snapshot_tree(self.output), before_tree)
                self.assertFalse(any(args[0] in ("publish", "build", "run")
                                     for args in self.dotnet_calls()[before_calls:]))
                self.assertEqual(self.signing_calls(), before_signing)

    def test_version_query_nonzero_with_valid_stdout_prevents_cleanup_and_publish(self):
        self.prepare_entrypoints()
        self.assert_success(self.run_entrypoint("app.sh", "package", "osx-arm64"))
        before_tree = self.snapshot_tree(self.output)
        for command in ("dist", "package", "all", "run-package"):
            with self.subTest(command=command):
                before = len(self.dotnet_calls())
                signing = self.signing_calls()
                result = self.run_entrypoint("app.sh", command,
                                             env={"TEST_VERSION_QUERY_FAIL": "1"})
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("intentional version query failure", result.stderr)
                self.assertEqual(self.dotnet_calls()[before:],
                                 [["--list-sdks"], self.version_query()])
                self.assertEqual(self.snapshot_tree(self.output), before_tree)
                self.assertEqual(self.signing_calls(), signing)
                self.assertNotIn("BUNDLE_HOST_MARKER", result.stdout)

    def test_helper_failures_preserve_previous_valid_bundle_and_archive(self):
        self.assert_success(self.run_helper())
        before = self.snapshot_tree(self.output)
        # A failed replacement must not silently overwrite the old payload.
        (self.publish / "NLog.config").write_bytes(b"replacement config")
        for phase, expected_calls in (("sign", 1), ("verify", 2), ("archive-verify", 3)):
            with self.subTest(phase=phase):
                count = len(self.signing_calls())
                result = self.run_helper(env={"TEST_CODESIGN_FAIL": phase})
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(len(self.signing_calls()) - count, expected_calls)
                self.assertEqual(self.snapshot_tree(self.output), before)
                self.assertFalse(list(self.output.glob(".macos-package.*")))
        # Validation failures must also leave already-published artifacts alone.
        (self.publish / "NLog.config").unlink()
        result = self.run_helper()
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.snapshot_tree(self.output), before)

    def test_archive_disables_appledouble_and_does_not_leak_staging(self):
        real_tar = shutil.which("tar", path=os.defpath)
        self.assertIsNotNone(real_tar)
        # Model macOS metadata handling even on hosts without copyfile support;
        # still delegate archive creation and extraction to the real tar binary.
        self.env["TEST_REAL_TAR"] = real_tar
        self.write_stub("tar", '''import os
import subprocess
import sys
if "-czf" in sys.argv and os.environ.get("COPYFILE_DISABLE") != "1":
    print("AppleDouble metadata was not disabled", file=sys.stderr)
    sys.exit(95)
sys.exit(subprocess.call([os.environ["TEST_REAL_TAR"], *sys.argv[1:]]))
''')
        xattr = shutil.which("xattr", path=os.defpath)
        if sys.platform == "darwin" and xattr:
            self.assert_success(subprocess.run(
                [xattr, "-w", "com.apple.test-packaging", "metadata",
                 str(self.publish / "NLog.config")],
                capture_output=True, text=True, timeout=10, check=False,
            ))
        self.assert_success(self.run_helper(env={"COPYFILE_DISABLE": "0"}))
        self.assert_bundle("osx-arm64")
        bundle, archive = self.bundle_paths()
        self.assertEqual(set(self.output.iterdir()), {bundle, archive})
        with tarfile.open(archive) as tar:
            for member in tar.getmembers():
                parts = PurePosixPath(member.name).parts
                self.assertFalse(any(part.startswith("._") or part == "__MACOSX"
                                     for part in parts), member.name)
                self.assertFalse(any(part.startswith(".macos-package.") or part == "verify"
                                     for part in parts), member.name)
                if member.issym():
                    self.assertNotIn(str(self.root), member.linkname)

    def test_legacy_corrupt_tar_preserves_input_and_cleans_extraction_directory(self):
        self.prepare_entrypoints()
        archive = self.output / f"MusicLyricApp-{VERSION}-osx-x64-mid.tar.gz"
        archive.write_bytes(b"not a gzip or tar archive\x00")
        before = self.snapshot_tree(self.output)
        result = self.run_entrypoint("build-macos-app.sh", VERSION)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.snapshot_tree(self.output), before)
        self.assertEqual(self.signing_calls(), [])
        self.assertEqual(self.dotnet_calls(), [])
        self.assertNotIn("Final macOS apps and archives are available", result.stdout)

    def test_publish_script_macos_uses_shared_helper_and_nonmac_retains_mid_archives(self):
        self.prepare_entrypoints()
        # publish.sh copies the source icon over the dotnet fixture's icon.
        self.fallback_icon.write_bytes(self.payload["Resources/app-logo.icns"])
        # Make the legacy Python zip fallback deterministic without requiring
        # a separate Python installation in the restricted stub PATH.
        self.write_stub("python3", '''import os
import sys
os.execv(sys.executable, [sys.executable, *sys.argv[1:]])
''')
        for host in ("Darwin", "Linux"):
            with self.subTest(host=host):
                before = len(self.dotnet_calls())
                signing_count = len(self.signing_calls())
                result = self.run_entrypoint("publish.sh", VERSION,
                                             env={"TEST_UNAME_OS": host})
                self.assert_success(result)
                calls = self.dotnet_calls()[before:]
                self.assertEqual([args[0] for args in calls], ["publish"] * 5)
                self.assertEqual([args[args.index("-r") + 1] for args in calls],
                                 ["win-x86", "win-x64", "linux-x64", "osx-x64", "osx-arm64"])
                for rid in ("osx-x64", "osx-arm64"):
                    bundle, archive = self.bundle_paths(rid)
                    mid = self.output / f"MusicLyricApp-{VERSION}-{rid}-mid.tar.gz"
                    self.assertFalse((self.output / rid).exists())
                    if host == "Darwin":
                        self.assert_bundle(rid)
                        self.assertFalse(mid.exists())
                    else:
                        self.assertFalse(bundle.exists())
                        self.assertFalse(archive.exists())
                        with tarfile.open(mid) as tar:
                            members = {str(PurePosixPath(member.name)): member
                                       for member in tar.getmembers()}
                            for name in ("MusicLyricApp", "NLog.config", "Resources/app-logo.icns"):
                                with tar.extractfile(members[name]) as stream:
                                    self.assertEqual(stream.read(), self.payload[name])
                            self.assertFalse(any(".app/" in name for name in members))
                self.assertEqual(len(self.signing_calls()) - signing_count,
                                 6 if host == "Darwin" else 0)
                self.assertFalse(list(self.output.glob(".macos-package.*")))
                self.assertNotIn("BUNDLE_HOST_MARKER", result.stdout)
                self.assertFalse((self.cwd / "publish").exists())

    def test_legacy_entrypoint_packages_both_mid_archives_from_script_directory(self):
        self.prepare_entrypoints()
        # Legacy input is an archive of published files, not a dotnet invocation.
        self.payload["MusicLyricApp"] = (self.publish / "MusicLyricApp").read_bytes()
        archives = [self.create_mid_archive(rid) for rid in ("osx-arm64", "osx-x64")]
        self.assert_success(self.run_entrypoint("build-macos-app.sh", VERSION))
        for rid in ("osx-arm64", "osx-x64"):
            with self.subTest(rid=rid):
                self.assert_bundle(rid)
        for archive in archives:
            self.assertFalse(archive.exists(), f"Intermediate archive not removed: {archive}")
        self.assertFalse((self.cwd / "publish").exists())
        self.assertEqual(self.dotnet_calls(), [])
        self.assertEqual(sum("--verify" in args for args in self.signing_calls()), 4)

    def test_legacy_entrypoint_missing_mid_archives_fails(self):
        self.prepare_entrypoints()
        result = self.run_entrypoint("build-macos-app.sh", VERSION)
        for rid in ("osx-arm64", "osx-x64"):
            self.assert_failed_without_artifacts(result, rid)
        self.assertEqual(self.signing_calls(), [])
        self.assertEqual(self.dotnet_calls(), [])

    def test_legacy_entrypoint_preserves_mid_archive_on_signing_failure(self):
        self.prepare_entrypoints()
        archive = self.create_mid_archive("osx-x64")
        original = archive.read_bytes()
        result = self.run_entrypoint("build-macos-app.sh", VERSION,
                                     env={"TEST_CODESIGN_FAIL": "sign"})
        self.assert_failed_without_artifacts(result, "osx-x64")
        self.assertEqual(archive.read_bytes(), original)
        self.assertTrue(self.signing_calls(), "Signing failure stub was never called")
        self.assertEqual(self.dotnet_calls(), [])


if __name__ == "__main__":
    unittest.main()
