import { expect, type Locator, type Page } from "@playwright/test";

export class RSSBokmarkrPage {
  readonly pageURL: string;
  readonly page: Page;
  readonly urlInput: Locator;
  readonly addURLButton: Locator;
  readonly deleteURLButton: Locator;
  readonly loginFormTitle: Locator;
  readonly loginUsernameField: Locator;
  readonly loginPasswordField: Locator;
  readonly saveURLsButton: Locator;
  readonly logoutButton: Locator;
  readonly subscribeModalButton: Locator;
  readonly subscribeFormButton: Locator;
  readonly subscribeEmailField: Locator;
  readonly unsubscribeButton: Locator;
  readonly loginModalButton: Locator;
  readonly loginFormButton: Locator;
  readonly skeletonLoading: Locator;

  private loginResonse?: any = undefined;

  readonly sessionKey = "session_id";

  constructor(pageURL: string, page: Page) {
    this.pageURL = pageURL;
    this.page = page;
    this.urlInput = page.getByPlaceholder("https://overreacted.io/rss.xml");
    this.addURLButton = page.getByRole("button", { name: "Add" });
    this.deleteURLButton = page.getByRole("button", { name: "X" });
    this.loginFormTitle = page.getByRole("heading", { name: "Log In Form" });
    this.loginUsernameField = page.getByPlaceholder("Username");
    this.loginPasswordField = page.getByPlaceholder("******");
    this.saveURLsButton = page.getByRole("button", { name: "Save Urls" });
    this.logoutButton = page.getByText("Log Out");
    this.subscribeModalButton = page.getByText("Subscribe").first();
    this.subscribeFormButton = page.getByText("Subscribe").nth(2);
    this.subscribeEmailField = page.getByPlaceholder("email@domain.com");
    this.unsubscribeButton = page.getByRole("button", { name: "Unsubscribe" });
    this.loginModalButton = page
      .locator("div")
      .filter({ hasText: /^Log In$/ })
      .locator("label");
    this.loginFormButton = page
      .locator("form")
      .getByText("Log In", { exact: true });
    this.skeletonLoading = page.locator(".skeleton");
  }

  async goto(urls?: string[]): Promise<void> {
    if (urls !== undefined) {
      await this.assertRSSList({
        expectedPostData: [urls],
        trigger: async () => {
          await this.page.goto(`${this.pageURL}/?url=${urls.join(",")}`);
        },
      });
      return;
    }

    await this.page.goto(this.pageURL);
  }

  async expectHaveURLParam(urls: string[]) {
    await expect(this.page).toHaveURL(`${this.pageURL}/?url=${urls.join(",")}`);
  }

  async expectNotHaveURLParam() {
    await expect(this.page).toHaveURL(this.pageURL);
  }

  async waitLoadingPage() {
    const firstSkeletonLoading = this.skeletonLoading.first();
    await firstSkeletonLoading.waitFor({ state: "visible" });
    await firstSkeletonLoading.waitFor({ state: "detached" });
  }

  async addURL(url: string) {
    await this.urlInput.fill(url);

    await this.assertRSSList({
      expectedPostData: [[url]],
      trigger: async () => {
        await this.addURLButton.click();
      },
    });

    await expect(this.urlInput).toHaveValue("");
  }

  async deleteURL(nth?: number) {
    if (nth !== undefined) {
      await this.deleteURLButton.nth(nth - 1).click();
      return;
    }
    await this.deleteURLButton.first().click();
  }

  async saveURLs(expectedURLs: string[]) {
    await this.assertSaveRSSUrls({
      expectedPostData: [
        { UserId: this.loginResonse?.["UserId"], Urls: expectedURLs },
      ],
      trigger: async () => {
        await this.saveURLsButton.click();
      },
    });
  }

  async subscribe(email: string) {
    await this.subscribeModalButton.click();
    await this.subscribeEmailField.fill(email);

    await this.assertSubscribe({
      expectedPostData: [
        { UserId: this.loginResonse?.["UserId"], Email: email },
      ],
      trigger: async () => {
        await this.subscribeFormButton.click();
      },
    });
  }

  async unsubscribe(email: string) {
    await this.assertUnsubscribe({
      expectedPostData: [{ Email: email }],
      trigger: async () => {
        await this.unsubscribeButton.click();
      },
    });
    await expect(this.unsubscribeButton).not.toBeAttached();
  }

  async login(username: string, password: string) {
    await this.loginModalButton.click();
    await expect(this.loginFormTitle).toBeVisible();

    await this.loginUsernameField.fill(username);
    await this.loginPasswordField.fill(password);

    const loginResponse = await this.assertLoginOrRegister({
      expectedPostData: [{ Username: username, Password: password }],
      trigger: async () => {
        await this.loginFormButton.click();
      },
    });
    await this.assertLocalStorageExist(this.sessionKey);

    this.loginResonse = loginResponse["Success"];
  }

  async logout() {
    await this.logoutButton.click();
    await this.assertLocalStorageNotExist(this.sessionKey);

    this.loginResonse = undefined;
  }

  async assertRSSList(option: {
    trigger: () => Promise<void>;
    expectedPostData: [string[]];
  }) {
    return await this.assertAPICall({
      ...option,
      url: "**/rpc/IRPCStore/getRSSList",
    });
  }

  async assertSaveRSSUrls(option: {
    trigger: () => Promise<void>;
    expectedPostData: [{ UserId: string; Urls: string[] }];
  }) {
    return await this.assertAPICall({
      ...option,
      url: "**/rpc/IRPCStore/saveRSSUrls",
    });
  }

  async assertSubscribe(option: {
    trigger: () => Promise<void>;
    expectedPostData: [{ UserId: string; Email: string }];
  }) {
    return await this.assertAPICall({
      ...option,
      url: "**/rpc/IRPCStore/subscribe",
    });
  }

  async assertUnsubscribe(option: {
    trigger: () => Promise<void>;
    expectedPostData: [{ Email: string }];
  }) {
    return await this.assertAPICall({
      ...option,
      url: "**/rpc/IRPCStore/unsubscribe",
    });
  }

  async assertLoginOrRegister(option: {
    trigger: () => Promise<void>;
    expectedPostData: [{ Username: string; Password: string }];
  }) {
    return await this.assertAPICall({
      ...option,
      url: "**/rpc/IRPCStore/loginOrRegister",
    });
  }

  async assertAPICall(option: {
    url: string;

    trigger: () => Promise<void>;
    expectedPostData: unknown;
  }) {
    const { url, trigger, expectedPostData } = option;

    const responsePromise = this.page.waitForResponse(url);

    await trigger();

    const response = await responsePromise;
    expect(response.ok()).toStrictEqual(true);
    expect(response.request().postDataJSON()).toStrictEqual(expectedPostData);

    return response.json();
  }

  async assertLocalStorageExist(key: string) {
    await this.page.waitForFunction((key) => {
      return localStorage[key] !== undefined;
    }, key);
  }

  async assertLocalStorageNotExist(key: string) {
    await this.page.waitForFunction((key) => {
      return localStorage[key] === undefined;
    }, key);
  }
}
