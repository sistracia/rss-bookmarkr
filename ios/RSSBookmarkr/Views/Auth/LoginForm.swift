import SwiftUI

struct LoginForm: View {
    var onLogIn: ((String, String) -> Void)
    
    @State private var username = ""
    @State private var password = ""
    
    var body: some View {
        Form {
            Section {
                TextField(text: $username, prompt: Text("Username")) {
                    Text("Username")
                }
                .textInputAutocapitalization(.never)
                SecureField(text: $password, prompt: Text("*****")) {
                    Text("Password")
                }
            }
            Section {
                Button {
                    onLogIn(username, password)
                } label: {
                    Text("Log In")
                }
                .frame(maxWidth: .infinity, alignment: .center)
            } footer: {
                Text("Account will be automatically created if not exist.")
            }
        }
        .listSectionSpacing(8)
        .frame(height: 200)
    }
}

#Preview {
    LoginForm { _, __ in }
}
