import CoreGraphics
import Foundation
let requiredPid = ProcessInfo.processInfo.environment["MLA_PID"].flatMap { Int($0) }
func pidOK(_ w: [String: Any]) -> Bool { requiredPid == nil || (w[kCGWindowOwnerPID as String] as? Int) == requiredPid }
// Usage: scroll <owner> <x> <y> <lines>   (negative lines scroll down)
let a = CommandLine.arguments
let list = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
guard let w = list.first(where: { ($0[kCGWindowOwnerName as String] as? String) == a[1] && ($0[kCGWindowLayer as String] as? Int) == 0 && pidOK($0) }),
      let b = w[kCGWindowBounds as String] as? [String: Any] else { exit(1) }
let p = CGPoint(x: (b["X"] as! Double) + Double(a[2])!, y: (b["Y"] as! Double) + Double(a[3])!)
CGEvent(mouseEventSource: nil, mouseType: .mouseMoved, mouseCursorPosition: p, mouseButton: .left)?.post(tap: .cghidEventTap)
usleep(100_000)
for _ in 0..<abs(Int32(a[4])!) {
    CGEvent(scrollWheelEvent2Source: nil, units: .line, wheelCount: 1, wheel1: Int32(a[4])! > 0 ? 5 : -5, wheel2: 0, wheel3: 0)?.post(tap: .cghidEventTap)
    usleep(30_000)
}
