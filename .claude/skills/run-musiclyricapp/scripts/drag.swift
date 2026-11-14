import CoreGraphics
import Foundation
let requiredPid = ProcessInfo.processInfo.environment["MLA_PID"].flatMap { Int($0) }
func pidOK(_ w: [String: Any]) -> Bool { requiredPid == nil || (w[kCGWindowOwnerPID as String] as? Int) == requiredPid }
// Usage: drag <owner> <x> <y> <dx> <dy>   (start point in window points; moves by dx,dy)
let a = CommandLine.arguments
let owner = a[1]; let sx = Double(a[2])!, sy = Double(a[3])!, dx = Double(a[4])!, dy = Double(a[5])!
func bounds() -> CGRect? {
    let list = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
    guard let w = list.first(where: { ($0[kCGWindowOwnerName as String] as? String) == owner && ($0[kCGWindowLayer as String] as? Int) == 0 && pidOK($0) }),
          let b = w[kCGWindowBounds as String] as? [String: Any] else { return nil }
    return CGRect(x: b["X"] as! Double, y: b["Y"] as! Double, width: b["Width"] as! Double, height: b["Height"] as! Double)
}
guard let r = bounds() else { print("no window"); exit(1) }
print("before \(r.origin)")
let start = CGPoint(x: r.minX + sx, y: r.minY + sy)
func post(_ t: CGEventType, _ p: CGPoint) { CGEvent(mouseEventSource: nil, mouseType: t, mouseCursorPosition: p, mouseButton: .left)?.post(tap: .cghidEventTap); usleep(20_000) }
post(.mouseMoved, start); post(.leftMouseDown, start); usleep(300_000)
for i in 1...40 { usleep(15_000); post(.leftMouseDragged, CGPoint(x: start.x + dx * Double(i) / 40, y: start.y + dy * Double(i) / 40)) }
post(.leftMouseUp, CGPoint(x: start.x + dx, y: start.y + dy)); usleep(300_000)
print("after  \(bounds()!.origin)")
