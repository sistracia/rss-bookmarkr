import SwiftUI

struct LoginForm: View {
    var onSucces: (() -> Void)?
    @Environment(ModelData.self) var modelData
    @State private var username = ""
    @State private var password = ""
    
    init(onSucces: (() -> Void)? = nil) {
        self.onSucces = onSucces
    }
    
    var body: some View {
        Form {
            Section {
                TextField(text: $username, prompt: Text("Username")) {
                    Text("Username")
                }
                SecureField(text: $password, prompt: Text("*****")) {
                    Text("Password")
                }
            }
            Section {
                Button {
                    Task {
                        try await modelData.setProfile(username: username, password: password)
                        username = ""
                        password = ""
                        if let onSucces = onSucces {
                            onSucces()
                        }
                    }
                } label: {
                    Text("Log In")
                }
                .frame(maxWidth: .infinity, alignment: .center)
            } footer: {
                Text("Account will be automatically created if not exist.")
            }
        }
        .listSectionSpacing(8)
    }
}

#Preview {
    LoginForm()
        .environment(ModelData())
}
