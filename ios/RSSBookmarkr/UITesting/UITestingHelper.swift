#if DEBUG
import Foundation

struct UITestingHelpers {
    static var isUITesting: Bool {
        ProcessInfo.processInfo.arguments.contains("-ui-testing")
    }

    static var apiURL: String? {
        ProcessInfo.processInfo.environment["API_URL"]
    }
}
#endif
