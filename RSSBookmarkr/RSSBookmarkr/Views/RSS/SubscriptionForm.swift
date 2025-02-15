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
            }
        }
        .listSectionSpacing(8)
    }
}

#Preview {
    SubscriptionForm { _ in }
}
