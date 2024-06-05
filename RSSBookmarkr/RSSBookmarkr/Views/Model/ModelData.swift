import Foundation

@Observable
class ModelData {
    var profile: Profile?
    
    func setProfile(username: String, password: String) async throws {
        self.profile = Profile(username: username, password: password)
    }
    
    func unsetProfile() {
        self.profile = nil
    }
}
