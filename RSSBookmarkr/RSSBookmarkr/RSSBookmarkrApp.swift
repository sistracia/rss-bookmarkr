import SwiftUI

@main
struct RSSBookmarkrApp: App {
    @State private var modelData: ModelData
    
    init () {
        var url = "https://rssbookmarkr.sistracia.com/rpc/IRPCStore/"
#if DEBUG
        if let apiUrl = UITestingHelpers.apiURL {
            url = apiUrl
        }
        
        if let bundleID = Bundle.main.bundleIdentifier, UITestingHelpers.isUITesting {
            UserDefaults.standard.removePersistentDomain(forName: bundleID)
        }
#endif
        modelData = ModelData(rssBookrmarkrClient: RSSBookmarkrClient(baseURL: URL(string: url)!))
    }
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environment(modelData)
        }
    }
}
