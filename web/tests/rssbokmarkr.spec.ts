import { test, expect, type Page } from "@playwright/test";

const pageURL = process.env.PAGE_URL || "http://localhost:8080";

const username = "testingusername";
const password = "testingpassword";
const sessionKey = "session_id";
const subscribeEmail = "test@test.com";

const rssURLS = [
  "https://overreacted.io/rss.xml",
  "https://feed.infoq.com/",
  "https://stackoverflow.blog/feed/",
];
const [overreactedURL, infoqURL, stackoverflowURL] = rssURLS;

async function assertRSSList(option: {
  page: Page;
  trigger: () => Promise<void>;
  expectedPostData: unknown;
}) {
  return await assertAPICallUnsubscribe({
    ...option,
    url: "**/rpc/IRPCStore/getRSSList",
  });
}

async function assertSaveRSSUrls(option: {
  page: Page;
  trigger: () => Promise<void>;
  expectedPostData: unknown;
}) {
  return await assertAPICallUnsubscribe({
    ...option,
    url: "**/rpc/IRPCStore/saveRSSUrls",
  });
}

async function assertSubscribe(option: {
  page: Page;
  trigger: () => Promise<void>;
  expectedPostData: unknown;
}) {
  return await assertAPICallUnsubscribe({
    ...option,
    url: "**/rpc/IRPCStore/subscribe",
  });
}

async function assertUnsubscribe(option: {
  page: Page;
  trigger: () => Promise<void>;
  expectedPostData: unknown;
}) {
  return await assertAPICallUnsubscribe({
    ...option,
    url: "**/rpc/IRPCStore/unsubscribe",
  });
}

async function assertLoginOrRegister(option: {
  page: Page;
  trigger: () => Promise<void>;
  expectedPostData: unknown;
}) {
  return await assertAPICallUnsubscribe({
    ...option,
    url: "**/rpc/IRPCStore/loginOrRegister",
  });
}

async function assertAPICallUnsubscribe(option: {
  url: string;
  page: Page;
  trigger: () => Promise<void>;
  expectedPostData: unknown;
}) {
  const { url, page, trigger, expectedPostData } = option;

  const responsePromise = page.waitForResponse(url);

  await trigger();

  const response = await responsePromise;
  expect(response.ok()).toStrictEqual(true);
  expect(response.request().postDataJSON()).toStrictEqual(expectedPostData);

  return response.json();
}

function getURLInput(page: Page) {
  return page.getByPlaceholder("https://overreacted.io/rss.xml");
}

function getAddURLButton(page: Page) {
  return page.getByRole("button", { name: "Add" });
}

function getDeleteURLButton(page: Page) {
  return page.getByRole("button", { name: "X" });
}

function getLoginFormTitle(page: Page) {
  return page.getByRole("heading", { name: "Log In Form" });
}

function getLoginUsernameField(page: Page) {
  return page.getByPlaceholder("Username");
}

function getLoginPasswordField(page: Page) {
  return page.getByPlaceholder("******");
}

function getSaveURLsButton(page: Page) {
  return page.getByRole("button", { name: "Save Urls" });
}

function getLogoutButton(page: Page) {
  return page.getByText("Log Out");
}

function getSubscribeModalButton(page: Page) {
  return page.getByText("Subscribe").first();
}

function getSubscribeFormButton(page: Page) {
  return page.getByText("Subscribe").nth(2);
}

function getSubscribeEmailField(page: Page) {
  return page.getByPlaceholder("email@domain.com");
}

function getUnsubscribeButton(page: Page) {
  return page.getByRole("button", { name: "Unsubscribe" });
}

function getLoginModalButton(page: Page) {
  return page
    .locator("div")
    .filter({ hasText: /^Log In$/ })
    .locator("label");
}

function getLoginFormButton(page: Page) {
  return page.locator("form").getByText("Log In", { exact: true });
}

async function waitLoadingPage(page: Page) {
  await page.locator(".skeleton").first().waitFor({ state: "visible" });
  await page.locator(".skeleton").first().waitFor({ state: "detached" });
}

async function assertLocalStorageExist(page: Page, key: string) {
  await page.waitForFunction((key) => {
    return localStorage[key] !== undefined;
  }, key);
}

async function assertLocalStorageNotExist(page: Page, key: string) {
  await page.waitForFunction((key) => {
    return localStorage[key] === undefined;
  }, key);
}

async function login(page: Page, username: string, password: string) {
  const loginResponse = await assertLoginOrRegister({
    page,
    expectedPostData: [{ Username: username, Password: password }],
    trigger: async () => {
      await getLoginFormButton(page).click();
    },
  });

  return loginResponse["Success"];
}

test("has title", async ({ page }) => {
  await page.goto(pageURL);
  await expect(page).toHaveTitle("RSS Bookmarkr");
});

test.describe("single url", () => {
  test("add new url", async ({ page }) => {
    await page.goto(pageURL);

    const urlInput = getURLInput(page);
    await urlInput.fill(overreactedURL);

    await assertRSSList({
      page,
      expectedPostData: [[overreactedURL]],
      trigger: async () => {
        await getAddURLButton(page).click();
      },
    });

    await expect(urlInput).toHaveValue("");
    await expect(page.getByText(overreactedURL)).toBeVisible();
    await expect(page).toHaveURL(`${pageURL}/?url=${overreactedURL}`);
  });

  test("init url with query param", async ({ page }) => {
    await assertRSSList({
      page,
      expectedPostData: [[overreactedURL]],
      trigger: async () => {
        await page.goto(`${pageURL}/?url=${overreactedURL}`);
      },
    });

    const urlInput = getURLInput(page);

    await expect(urlInput).toHaveValue("");
    await expect(page.getByText(overreactedURL)).toBeVisible();
    await expect(page).toHaveURL(`${pageURL}/?url=${overreactedURL}`);
  });

  test("delete url", async ({ page }) => {
    await page.goto(`${pageURL}/?url=${overreactedURL}`);

    await getDeleteURLButton(page).click();
    await expect(page).toHaveURL(pageURL);
  });
});

test.describe("multiple url", () => {
  test("add new url", async ({ page }) => {
    await page.goto(pageURL);

    const urlInput = getURLInput(page);
    const addButton = getAddURLButton(page);

    for (const rssURL of rssURLS) {
      await urlInput.fill(rssURL);

      await assertRSSList({
        page,
        expectedPostData: [[rssURL]],
        trigger: async () => {
          await addButton.click();
        },
      });

      await expect(page.getByText(rssURL)).toBeVisible();
    }

    await expect(page).toHaveURL(`${pageURL}/?url=${rssURLS.join(",")}`);
  });

  test("init url with query param", async ({ page }) => {
    await page.goto(`${pageURL}/?url=${rssURLS.join(",")}`);

    await waitLoadingPage(page);
    for (const rssURL of rssURLS) {
      await expect(page.getByText(rssURL)).toBeVisible();
    }

    await expect(page).toHaveURL(`${pageURL}/?url=${rssURLS.join(",")}`);
  });

  test("delete url", async ({ page }) => {
    await page.goto(`${pageURL}/?url=${rssURLS.join(",")}`);

    await getDeleteURLButton(page).first().click();
    await expect(page.getByText(infoqURL)).toBeVisible();
    await expect(page.getByText(stackoverflowURL)).toBeVisible();
    await expect(page).toHaveURL(
      `${pageURL}/?url=${infoqURL},${stackoverflowURL}`
    );
  });
});

test.describe("user authentication", () => {
  test("login and logout", async ({ page }) => {
    await page.goto(pageURL);

    await getLoginModalButton(page).click();
    await expect(getLoginFormTitle(page)).toBeVisible();

    await getLoginUsernameField(page).fill(username);
    await getLoginPasswordField(page).fill(password);
    await login(page, username, password);
    await assertLocalStorageExist(page, sessionKey);

    await getLogoutButton(page).click();
    await assertLocalStorageNotExist(page, sessionKey);
  });
});

test.describe("authorized user", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(pageURL);

    await getLoginModalButton(page).click();

    await getLoginUsernameField(page).fill(username);
    await getLoginPasswordField(page).fill(password);
  });

  test.afterEach(async ({ page }) => {
    await getLogoutButton(page).click();
    await assertLocalStorageNotExist(page, sessionKey);
  });

  test("save urls and delete the url", async ({ page }) => {
    const { UserId: userId } = await login(page, username, password);

    await getURLInput(page).fill(overreactedURL);
    await getAddURLButton(page).click();
    await waitLoadingPage(page);

    const saveUrlsButton = getSaveURLsButton(page);

    await assertSaveRSSUrls({
      page,
      expectedPostData: [[userId, [overreactedURL]]],
      trigger: async () => {
        await saveUrlsButton.click();
      },
    });

    await getDeleteURLButton(page).click();

    await assertSaveRSSUrls({
      page,
      expectedPostData: [[userId, []]],
      trigger: async () => {
        await saveUrlsButton.click();
      },
    });
  });

  test("subscribe and unsubscribe", async ({ page }) => {
    const { UserId: userId } = await login(page, username, password);

    await getSubscribeModalButton(page).click();
    await getSubscribeEmailField(page).fill(subscribeEmail);

    await assertSubscribe({
      page,
      expectedPostData: [[userId, subscribeEmail]],
      trigger: async () => {
        await getSubscribeFormButton(page).click();
      },
    });

    await assertUnsubscribe({
      page,
      expectedPostData: [subscribeEmail],
      trigger: async () => {
        await getUnsubscribeButton(page).click();
      },
    });
  });
});
