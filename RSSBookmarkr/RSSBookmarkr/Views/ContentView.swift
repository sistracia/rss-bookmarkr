import SwiftUI

struct ContentView: View {
    @Environment(ModelData.self) var modelData
    @State private var showLoginSheet = false
    
    @AppStorage("sessionId") var sessionId: String?
    
    
    var body: some View {
        let loading = modelData.serverState == .loading
        
        return NavigationStack {
            VStack {
                ScrollView {
                    SearchBar()
                    ForEach(modelData.rssList) { rssItem in
                        RSSCard(rss: rssItem)
                            .redacted(reason: loading ? .placeholder : [])
                            .shimmer(show: loading)
                    }
                }
                .padding(.horizontal, 5)
            }
            .navigationTitle("RSS Bookmarkr")
            .toolbar{
                if modelData.user == nil {
                    Button {
                        showLoginSheet.toggle()
                    } label: {
                        Text("Log In")
                    }
                } else {
                    Button {
                        modelData.logout()
                        sessionId = modelData.user?.userId
                    } label: {
                        Text("Log Out")
                    }
                }
            }
            .sheet(isPresented: $showLoginSheet) {
                NavigationStack {
                    LoginForm(sessionId: $sessionId) {
                        showLoginSheet.toggle()
                    }
                }
                .presentationDetents([.height(200)])
            }
        }
        .task {
            if let sessionId = sessionId {
                await modelData.initUser(sessionId: sessionId)
            }
        }
    }
}

#Preview {
    @Previewable @State var modelData = ModelData()
    modelData.urls = [
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
    ]
    modelData.rssList = [
        .placeholder,
        .placeholder,
        .placeholder,
        .placeholder,
        .placeholder,
        .placeholder
    ]
    return ContentView().environment(modelData)
}
