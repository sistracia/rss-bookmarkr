import SwiftUI

struct SubscriptionForm: View {
    var onSubscribe: ((String) -> Void)
    
    @State private var email = ""
    
    var body: some View {
        Form {
            Section {
                TextField(text: $email, prompt: Text(verbatim: "email@example.com")) {
                    Text("E-Mail")
                }
                .keyboardType(.emailAddress)
                .textInputAutocapitalization(.never)
            }
            Section {
                Button {
                    onSubscribe(email)
                } label: {
                    Text("Subscribe")
                }
                .frame(maxWidth: .infinity, alignment: .center)
            } footer: {
                Text("Email notification will be sent everyday.")
            }
        }
        .listSectionSpacing(8)
        .frame(height: 155)
    }
}

#Preview {
    SubscriptionForm { _ in }
}
