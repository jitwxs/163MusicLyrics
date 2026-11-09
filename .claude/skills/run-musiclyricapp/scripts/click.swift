import CoreGraphics
import Foundation
let requiredPid = ProcessInfo.processInfo.environment["MLA_PID"].flatMap { Int($0) }
func pidOK(_ w: [String: Any]) -> Bool { requiredPid == nil || (w[kCGWindowOwnerPID as String] as? Int) == requiredPid }
// Usage: click <owner> <x> <y>   (x,y in window points, relative to the window's top-left)
let owner = CommandLine.arguments[1]
let dx = Double(CommandLine.arguments[2])!, dy = Double(CommandLine.arguments[3])!
let list = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
guard let w = list.first(where: { ($0[kCGWindowOwnerName as String] as? String) == owner && ($0[kCGWindowLayer as String] as? Int) == 0 && pidOK($0) }),
      let b = w[kCGWindowBounds as String] as? [String: Any],
      let x = b["X"] as? Double, let y = b["Y"] as? Double else { print("window not found"); exit(1) }
let p = CGPoint(x: x + dx, y: y + dy)
for type in [CGEventType.leftMouseDown, .leftMouseUp] {
    CGEvent(mouseEventSource: nil, mouseType: type, mouseCursorPosition: p, mouseButton: .left)?.post(tap: .cghidEventTap)
    usleep(60_000)
}
print("clicked \(p)")
