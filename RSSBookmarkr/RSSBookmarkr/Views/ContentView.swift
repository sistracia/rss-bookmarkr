import SwiftUI

struct ContentView: View {
    @Environment(ModelData.self) var modelData
    @State private var showLoginSheet = false
    
    var body: some View {
        NavigationStack {
            VStack {
                Image(systemName: "globe")
                    .imageScale(.large)
                    .foregroundStyle(.tint)
                Text("Hello, world!")
            }
            .padding()
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
    return ContentView().environment(modelData)
}
