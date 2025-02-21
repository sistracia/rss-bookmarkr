
import XCTest

struct RSSBookmarkrScreen {
    let app: XCUIApplication
    
    init(app: XCUIApplication) {
        self.app = app
    }
    
    func urlInput() -> XCUIElement {
        app.textFields["https://overreacted.io/rss.xml"]
    }
    
    func addURLButton () -> XCUIElement {
        app.buttons["Add"]
    }
    
    func deleteURLButton (_ nth: Int = 0) -> XCUIElement {
        app.buttons.matching(identifier: "Delete Button").element(boundBy: nth)
    }
    
    func saveURLsButton () -> XCUIElement {
        app.buttons["Save Urls"]
    }
    
    func loginModalButton () -> XCUIElement {
        app.buttons["Log In"]
    }
    
    func loginFormText() -> XCUIElement {
        app.staticTexts["Account will be automatically created if not exist."]
    }
    
    func loginUsernameField () -> XCUIElement {
        app.textFields["Username"]
    }
    
    func loginPasswordField () -> XCUIElement {
        app.secureTextFields["*****"]
    }
    
    func loginFormButton () -> XCUIElement {
        app.buttons.matching(identifier: "Log In").element(boundBy: 1)
    }
    
    func logoutButton () -> XCUIElement {
        app.buttons["Log Out"]
    }
    
    func subscribeModalButton () -> XCUIElement {
        app.buttons["Subscribe"]
    }
    
    func subscribeFormText() -> XCUIElement {
        app.staticTexts["Email notification will be sent everyday."]
    }

    func subscribeEmailField () -> XCUIElement {
        app.textFields["email@example.com"]
    }
    
    func subscribeFormButton () -> XCUIElement {
        app.buttons.matching(identifier: "Subscribe").element(boundBy: 1)
    }

    func unsubscribeButton () -> XCUIElement {
        app.buttons["Unsubscribe"]
    }
    
    func addUrl(_ url: String) {
        let urlInput =  self.urlInput()
        urlInput.tap()
        urlInput.typeText(url);
        
        let addURLButton = self.addURLButton()
        print(addURLButton.isEnabled)
        addURLButton.tap();
        print(addURLButton.isEnabled)
        XCTAssert(self.urlInput().value as? String != url)
    }
    
    func deleteUrl(_ nth: Int = 0) {
        self.deleteURLButton(nth).tap()
    }
    
    func login(username: String, password: String) {
        self.loginModalButton().tap();
        XCTAssert(self.loginFormText().waitForExistence(timeout: 5))
        
        let usernameField = self.loginUsernameField()
        usernameField.tap();
        usernameField.typeText(username);
        
        let passwordField = self.loginPasswordField()
        passwordField.tap();
        passwordField.typeText(password);
        
        self.loginFormButton().tap();
        XCTAssert(self.logoutButton().waitForExistence(timeout: 5))
    }
    
    func logout() {
        self.logoutButton().tap();
        XCTAssert(self.logoutButton().waitForNonExistence(timeout: 5))
    }
    
    func saveUrls() {
        self.saveURLsButton().tap();
        XCTAssert(self.app.staticTexts["Urls saved successfully"].waitForExistence(timeout: 5))
    }
    
    func subscribe(email: String) {
        self.subscribeModalButton().tap();
        XCTAssert(self.subscribeFormText().waitForExistence(timeout: 5))
        
        let emailField = self.subscribeEmailField()
        emailField.tap()
        emailField.typeText(email)
        
        self.subscribeFormButton().tap();
        XCTAssert(self.unsubscribeButton().waitForExistence(timeout: 5))
    }

    func unsubscribe() {
        self.unsubscribeButton().tap();
        XCTAssert(self.subscribeModalButton().waitForExistence(timeout: 5))
    }
}
