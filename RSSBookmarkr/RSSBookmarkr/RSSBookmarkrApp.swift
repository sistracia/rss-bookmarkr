import SwiftUI

@main
struct RSSBookmarkrApp: App {
    @State private var modelData = ModelData()
    var body: some Scene {
        WindowGroup {
            Text("Hello World")
                .environment(modelData)
//            ContentView()
//                .environment(modelData)
        }
    }
}
