import SwiftUI

struct ContentView: View {
    @Environment(ModelData.self) var modelData
    @State private var showLoginSheet = false
    @State private var showSubscriptionSheet = false
    @State private var showSavedUrlToast = false
    
    @AppStorage("sessionId") var sessionId: String?
    
    var error: (Bool, String) {
        switch modelData.serverState {
        case .error(let error):
            return (true, error)
        default:
            return (false, "")
        }
    }
    
    var isLoading: Bool {
        modelData.serverState == .loading
    }
    
    var body: some View {
        let showError: Binding = Binding(
            get: {
                return self.error.0
            },
            set: { showError in
                modelData.serverState = .idle
            }
        )
        
        NavigationStack {
            VStack {
                ScrollView {
                    SearchBar()
                    if let user = modelData.user {
                        HStack(spacing: 25) {
                            Button {
                                Task {
                                    await modelData.saveUrls()
                                    showSavedUrlToast = true
                                }
                            } label: {
                                Text("Save Urls")
                            }
                            
                            if user.email == "" {
                                Button {
                                    showSubscriptionSheet.toggle()
                                } label: {
                                    Text("Subscribe")
                                }
                            } else {
                                Button {
                                    Task {
                                        await modelData.unsubscribe()
                                    }
                                } label: {
                                    Text("Unsubscribe")
                                }
                            }
                            
                            Spacer()
                        }
                        .padding()
                    }
                    
                    ForEach(modelData.rssList) { rssItem in
                        RSSCard(rss: rssItem)
                            .redacted(reason: isLoading ? .placeholder : [])
                            .shimmer(show: isLoading)
                    }
                }
                .padding(.horizontal, 5)
            }
            .navigationTitle("RSS Bookmarkr")
            .toolbar {
                if modelData.user == nil {
                    Button {
                        showLoginSheet.toggle()
                    } label: {
                        Text("Log In")
                    }
                } else {
                    Button {
                        modelData.logout()
                        sessionId = nil
                    } label: {
                        Text("Log Out")
                    }
                }
            }
            .sheet(isPresented: $showLoginSheet) {
                NavigationStack {
                    LoginForm { username, password in
                        Task {
                            await modelData.login(
                                username: username, password: password)
                            sessionId = modelData.user?.sessionId
                            showLoginSheet.toggle()
                        }
                    }
                }
                .presentationDetents([.height(200)])
            }
            .sheet(isPresented: $showSubscriptionSheet) {
                NavigationStack {
                    SubscriptionForm { email in
                        Task {
                            await modelData.subscribe(email: email)
                            showSubscriptionSheet.toggle()
                        }
                    }
                }
                .presentationDetents([.height(150)])
            }
            
            .toast(message: error.1, isShowing: showError)
            .toast(message: "Urls saved successfully", isShowing: $showSavedUrlToast)
        }
        .task {
            if let sessionId = sessionId {
                await modelData.initUser(sessionId: sessionId)
            }
            
        }
    }
}

#Preview("Logged In") {
    @Previewable @State var modelData = ModelData(rssBookrmarkrClient: RSSBookmarkrClient(baseURL: URL(string: "https://rssbookmarkr.sistracia.com/rpc/IRPCStore/")!))
    modelData.user = User(
        userId: "userId", sessionId: "sessionId", email: "email@example.com")
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
        .placeholder,
    ]
    return ContentView().environment(modelData)
}

#Preview("Logged Out") {
    @Previewable @State var modelData = ModelData(rssBookrmarkrClient: RSSBookmarkrClient(baseURL: URL(string: "https://rssbookmarkr.sistracia.com/rpc/IRPCStore/")!))
    return ContentView().environment(modelData)
}
