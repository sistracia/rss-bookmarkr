import SwiftUI

struct ContentView: View {
    @Environment(ModelData.self) var modelData
    @State private var showLoginSheet = false
    
    var body: some View {
        NavigationStack {
            VStack {
                SearchBar()
                Spacer()
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
                    } label: {
                        Text("Log Out")
                    }
                }
            }
            .sheet(isPresented: $showLoginSheet) {
                NavigationStack {
                    LoginForm {
                        showLoginSheet.toggle()
                    }
                }
                .presentationDetents([.height(200)])
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
    return ContentView().environment(modelData)
}
