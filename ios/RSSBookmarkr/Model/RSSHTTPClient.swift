import Foundation

let validStatus = 200...299

protocol RSSHTTPClient {
    func httpData(for: URLRequest) async throws -> Data
}

extension URLSession: RSSHTTPClient {
    func httpData(for url: URLRequest) async throws -> Data {
        guard let (data, response) = try await self.data(for: url, delegate: nil) as? (Data, HTTPURLResponse),
              validStatus.contains(response.statusCode)
        else {
            throw RSSError.networkError
        }
        
        return data
    }
}
