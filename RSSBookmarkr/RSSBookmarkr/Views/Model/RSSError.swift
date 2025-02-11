import Foundation

enum RSSError: Error {
    case invalidURL
    case missingData
    case networkError
    case unexpectedError(error: Error)
}

extension RSSError: LocalizedError {
    var errorDescription: String? {
        switch self {
        case .invalidURL:
            return NSLocalizedString("Invalid URL", comment: "")
        case .missingData:
            return NSLocalizedString("No data received.", comment: "")
        case .networkError:
            return NSLocalizedString("Network error.", comment: "")
        case .unexpectedError(error: let error):
            return NSLocalizedString(
                "Unexpected error: \(error.localizedDescription)",
                comment: ""
            )
        }
    }
}
