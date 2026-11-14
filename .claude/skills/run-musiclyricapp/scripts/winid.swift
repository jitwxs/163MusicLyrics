import CoreGraphics
import Foundation
let requiredPid = ProcessInfo.processInfo.environment["MLA_PID"].flatMap { Int($0) }
func pidOK(_ w: [String: Any]) -> Bool { requiredPid == nil || (w[kCGWindowOwnerPID as String] as? Int) == requiredPid }
let owner = CommandLine.arguments.count > 1 ? CommandLine.arguments[1] : "MusicLyricApp"
let list = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
for w in list where (w[kCGWindowOwnerName as String] as? String) == owner && (w[kCGWindowLayer as String] as? Int) == 0 && pidOK(w) {
    let b = w[kCGWindowBounds as String] as? [String: Any] ?? [:]
    print("\(w[kCGWindowNumber as String]!) \(b["Width"] ?? 0)x\(b["Height"] ?? 0)")
}
