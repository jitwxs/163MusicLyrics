import CoreGraphics
import Foundation
// Usage: key <virtualKeyCode> [cmd|ctrl]
let code = CGKeyCode(Int(CommandLine.arguments[1])!)
var flags: CGEventFlags = []
if CommandLine.arguments.count > 2 { flags = CommandLine.arguments[2] == "cmd" ? .maskCommand : .maskControl }
for down in [true, false] {
    let e = CGEvent(keyboardEventSource: nil, virtualKey: code, keyDown: down)
    e?.flags = flags
    e?.post(tap: .cghidEventTap)
    usleep(50_000)
}
