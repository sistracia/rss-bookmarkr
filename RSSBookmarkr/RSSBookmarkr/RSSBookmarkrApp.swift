import SwiftUI
import UIKit
import BackgroundTasks

@main
struct RSSBookmarkrApp: App {
    @UIApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @Environment(\.scenePhase) private var scenePhase
    
    var url = "https://rssbookmarkr.sistracia.com/rpc/IRPCStore/"
    @State private var modelData: ModelData
    
    init () {
#if DEBUG
        if let apiUrl = UITestingHelpers.apiURL {
            url = apiUrl
        }
        
        if let bundleID = Bundle.main.bundleIdentifier, UITestingHelpers.isUITesting {
            UserDefaults.standard.removePersistentDomain(forName: bundleID)
        }
#endif
        modelData = ModelData(rssBookrmarkrClient: RSSBookmarkrClient(baseURL: URL(string: url)!))
    }
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environment(modelData)
        }
        .onChange(of: scenePhase) { oldScenePhase, newScenePhase in
            switch newScenePhase {
            case .background: scheduleAppRefresh()
            default: break
            }
        }
        .backgroundTask(.appRefresh("RSSFetcher")) {
            scheduleAppRefresh()
            await sendNotification()
        }
    }
}

class AppDelegate: NSObject, UIApplicationDelegate {
    func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
        var shouldRequestNotif = true
#if DEBUG
        shouldRequestNotif = !UITestingHelpers.isUITesting
#endif
        print(shouldRequestNotif)
        if shouldRequestNotif {
            Task {
                do {
                    try await UNUserNotificationCenter.current().requestAuthorization(options: [.alert])
                } catch {}
            }
        }
        return true
    }
}

func scheduleAppRefresh() {
    let today = Calendar.current.startOfDay(for: .now)
    let tomorrow = Calendar.current.date(byAdding: .day, value: 1, to: today)!
    let midnightComponent = DateComponents(hour: 0)
    let midnight = Calendar.current.date(byAdding: midnightComponent, to: tomorrow)
    
    let request = BGAppRefreshTaskRequest(identifier: "RSSFetcher")
    request.earliestBeginDate = midnight
    try? BGTaskScheduler.shared.submit(request)
    
}

func createNotification(content: UNMutableNotificationContent) async {
    // Obtain the notification settings.
    let settings = await UNUserNotificationCenter.current().notificationSettings()
    
    // Verify the authorization status.
    guard (settings.authorizationStatus == .authorized) ||
            (settings.authorizationStatus == .provisional) else { return }
    
    
    // show this notification five seconds from now
    let trigger = UNTimeIntervalNotificationTrigger(timeInterval: 5, repeats: false)
    
    // choose a random identifier
    let request = UNNotificationRequest(identifier: UUID().uuidString, content: content, trigger: trigger)
    
    // add our notification request
    do {
        try await UNUserNotificationCenter.current().add(request)
    } catch {}
}

func sendNotification() async {
    let content = UNMutableNotificationContent()
    content.title = "New RSS Release"
    //    content.subtitle = "Apple Concurrency, Apple OS, Apple Swift"
    //    content.interruptionLevel = .passive // Shows only in Notification Center & Lock Screen
    
    await createNotification(content: content)
}
