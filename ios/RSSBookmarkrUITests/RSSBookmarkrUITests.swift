import XCTest

final class RSSBookmarkrUITests: XCTestCase {
    private let rssURLs = [
        "https://feed.infoq.com/",
        "https://stackoverflow.blog/feed/"
    ]
    
    private var infoqURL: String { rssURLs[1] }
    private var stackoverflowURL: String { rssURLs[2] }
    
    override func setUpWithError() throws {
        // Put setup code here. This method is called before the invocation of each test method in the class.
        
        // In UI tests it is usually best to stop immediately when a failure occurs.
        continueAfterFailure = false
        
        // In UI tests it’s important to set the initial state - such as interface orientation - required for your tests before they run. The setUp method is a good place to do this.
    }
    
    override func tearDownWithError() throws {
        // Put teardown code here. This method is called after the invocation of each test method in the class.
    }
    
    private func generateRandomString() -> String {
        return String(UUID().uuidString.prefix(8)) // Generates a random string
    }
    
    private func generateUsername() -> String {
        return "\(generateRandomString())username"
    }
    
    private func generatePassword() -> String {
        return "\(generateRandomString())password"
    }
    
    private func generateEmail() -> String {
        return "\(generateRandomString())@email.com"
    }

    func testHasTitlte() throws {
        let app = XCUIApplication()
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        XCTAssert(app.staticTexts["RSS Bookmarkr"].exists)
        
    }
    
    func testAddSingleUrl() throws {
        let app = XCUIApplication()
        let screen = RSSBookmarkrScreen(app: app)
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        screen.addUrl(self.infoqURL)
    }
    
    func testDeleteSingleUrl() throws {
        let app = XCUIApplication()
        let screen = RSSBookmarkrScreen(app: app)
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        screen.addUrl(self.infoqURL)
        screen.deleteUrl()
        XCTAssert(!app.staticTexts[self.infoqURL].exists)
    }
    
    func testAddMultipleUrl() throws {
        let app = XCUIApplication()
        let screen = RSSBookmarkrScreen(app: app)
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        for url in self.rssURLs {
            screen.addUrl(url)
        }
    }
    
    func testDeleteMultipleUrl() throws {
        let app = XCUIApplication()
        let screen = RSSBookmarkrScreen(app: app)
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        for url in self.rssURLs {
            screen.addUrl(url)
        }
        
        for url in self.rssURLs {
            screen.deleteUrl()
            XCTAssert(!app.staticTexts[url].exists)
        }
    }
    
    func testLogin() throws {
        let app = XCUIApplication()
        let screen = RSSBookmarkrScreen(app: app)
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        screen.login(username: self.generateUsername(), password: self.generatePassword())
    }
    
    func testLogout() throws {
        let app = XCUIApplication()
        let screen = RSSBookmarkrScreen(app: app)
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        screen.login(username: self.generateUsername(), password: self.generatePassword())
        screen.logout()
    }
    
    func testSaveUrls() throws {
        let app = XCUIApplication()
        let screen = RSSBookmarkrScreen(app: app)
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        screen.login(username: self.generateUsername(), password: self.generatePassword())
        
        for url in self.rssURLs {
            screen.addUrl(url)
        }
        
        screen.saveUrls()
    }
    
    func testSubscribeUnsubscribe() throws {
        let app = XCUIApplication()
        let screen = RSSBookmarkrScreen(app: app)
        
        app.launchArguments += ["-ui-testing"]
        app.launch()
        
        screen.login(username: self.generateUsername(), password: self.generatePassword())
        screen.subscribe(email: self.generateEmail())
        screen.unsubscribe()
    }
}
