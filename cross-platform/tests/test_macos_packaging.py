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
        for name in ("app.sh", "build-macos-app.sh"):
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
