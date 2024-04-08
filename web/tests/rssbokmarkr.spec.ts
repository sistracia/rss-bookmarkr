import { test, expect } from "@playwright/test";

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

test("has title", async ({ page }) => {
  await page.goto(pageURL);
  await expect(page).toHaveTitle("RSS Bookmarkr");
});

test.describe("single url", () => {
  test("add new url", async ({ page }) => {
    await page.goto(pageURL);

    const urlInput = page.getByPlaceholder("https://overreacted.io/rss.xml");
    await urlInput.fill(overreactedURL);

    const rssListResponsePromise = page.waitForResponse(
      "**/rpc/IRPCStore/getRSSList"
    );
    await page.getByRole("button", { name: "Add" }).click();

    const rssListResponse = await rssListResponsePromise;
    expect(rssListResponse.ok()).toStrictEqual(true);
    expect(rssListResponse.request().postDataJSON()).toStrictEqual([
      [overreactedURL],
    ]);

    await expect(urlInput).toHaveValue("");
    await expect(page.getByText(overreactedURL)).toBeVisible();
    await expect(page).toHaveURL(`${pageURL}/?url=${overreactedURL}`);
  });

  test("init url with query param", async ({ page }) => {
    const rssListResponsePromise = page.waitForResponse(
      "**/rpc/IRPCStore/getRSSList"
    );
    await page.goto(`${pageURL}/?url=${overreactedURL}`);

    const rssListResponse = await rssListResponsePromise;
    expect(rssListResponse.ok()).toStrictEqual(true);
    expect(rssListResponse.request().postDataJSON()).toStrictEqual([
      [overreactedURL],
    ]);

    const urlInput = page.getByPlaceholder("https://overreacted.io/rss.xml");

    await expect(urlInput).toHaveValue("");
    await expect(page.getByText(overreactedURL)).toBeVisible();
    await expect(page).toHaveURL(`${pageURL}/?url=${overreactedURL}`);
  });

  test("delete url", async ({ page }) => {
    await page.goto(`${pageURL}/?url=${overreactedURL}`);

    await page.getByRole("button", { name: "X" }).click();
    await expect(page).toHaveURL(pageURL);
  });
});

test.describe("multiple url", () => {
  test("add new url", async ({ page }) => {
    await page.goto(pageURL);

    const urlInput = page.getByPlaceholder("https://overreacted.io/rss.xml");
    const addButton = page.getByRole("button", { name: "Add" });

    for (const rssURL of rssURLS) {
      await urlInput.fill(rssURL);

      const rssListResponsePromise = page.waitForResponse(
        "**/rpc/IRPCStore/getRSSList"
      );
      await addButton.click();

      const rssListResponse = await rssListResponsePromise;
      expect(rssListResponse.ok()).toStrictEqual(true);
      expect(rssListResponse.request().postDataJSON()).toStrictEqual([
        [rssURL],
      ]);

      await expect(page.getByText(rssURL)).toBeVisible();
    }

    await expect(page).toHaveURL(`${pageURL}/?url=${rssURLS.join(",")}`);
  });

  test("init url with query param", async ({ page }) => {
    await page.goto(`${pageURL}/?url=${rssURLS.join(",")}`);

    await page.locator(".skeleton").first().waitFor({ state: "visible" });
    await page.locator(".skeleton").first().waitFor({ state: "detached" });
    for (const rssURL of rssURLS) {
      await expect(page.getByText(rssURL)).toBeVisible();
    }

    await expect(page).toHaveURL(`${pageURL}/?url=${rssURLS.join(",")}`);
  });

  test("delete url", async ({ page }) => {
    await page.goto(`${pageURL}/?url=${rssURLS.join(",")}`);

    await page.getByRole("button", { name: "X" }).first().click();
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

    await page
      .locator("div")
      .filter({ hasText: /^Log In$/ })
      .locator("label")
      .click();
    await expect(
      page.getByRole("heading", { name: "Log In Form" })
    ).toBeVisible();

    await page.getByPlaceholder("Username").fill(username);
    await page.getByPlaceholder("******").fill(password);
    await page.locator("form").getByText("Log In", { exact: true }).click();
    await page.waitForFunction((sessionKey) => {
      return localStorage[sessionKey] !== undefined;
    }, sessionKey);

    await page.getByText("Log Out").click();
    await page.waitForFunction((sessionKey) => {
      return localStorage[sessionKey] === undefined;
    }, sessionKey);
  });
});

test.describe("authorized user", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(pageURL);

    await page
      .locator("div")
      .filter({ hasText: /^Log In$/ })
      .locator("label")
      .click();

    await page.getByPlaceholder("Username").fill(username);
    await page.getByPlaceholder("******").fill(password);
    await page.locator("form").getByText("Log In", { exact: true }).click();
    await page.waitForFunction((sessionKey) => {
      return localStorage[sessionKey] !== undefined;
    }, sessionKey);
  });

  test.afterEach(async ({ page }) => {
    await page.getByText("Log Out").click();
    await page.waitForFunction((sessionKey) => {
      return localStorage[sessionKey] === undefined;
    }, sessionKey);
  });

  test("save urls and delete the url", async ({ page }) => {
    await page
      .getByPlaceholder("https://overreacted.io/rss.xml")
      .fill(overreactedURL);
    await page.getByRole("button", { name: "Add" }).click();
    await page.locator(".skeleton").first().waitFor({ state: "visible" });
    await page.locator(".skeleton").first().waitFor({ state: "detached" });

    let rssSaveResponsePromise = page.waitForResponse(
      "**/rpc/IRPCStore/saveRSSUrls"
    );
    await page.getByRole("button", { name: "Save Urls" }).click();

    let rssSaveResponse = await rssSaveResponsePromise;
    expect(rssSaveResponse.ok()).toStrictEqual(true);
    expect(rssSaveResponse.request().postDataJSON()).toStrictEqual([
      [expect.stringContaining("-"), [overreactedURL]],
    ]);

    rssSaveResponsePromise = page.waitForResponse(
      "**/rpc/IRPCStore/saveRSSUrls"
    );

    await page.getByRole("button", { name: "X" }).click();
    await page.getByRole("button", { name: "Save Urls" }).click();

    rssSaveResponse = await rssSaveResponsePromise;
    expect(rssSaveResponse.ok()).toStrictEqual(true);
    expect(rssSaveResponse.request().postDataJSON()).toStrictEqual([
      [expect.stringContaining("-"), []],
    ]);
  });

  test("subscribe and unsubscribe", async ({ page }) => {
    await page.getByText("Subscribe").first().click();
    await page.getByPlaceholder("email@domain.com").fill(subscribeEmail);

    const subscribeResponsePromise = page.waitForResponse(
      "**/rpc/IRPCStore/subscribe"
    );
    await page.getByText("Subscribe").nth(2).click();

    const subscribeResponse = await subscribeResponsePromise;
    expect(subscribeResponse.ok()).toStrictEqual(true);
    expect(subscribeResponse.request().postDataJSON()).toStrictEqual([
      [expect.stringContaining("-"), subscribeEmail],
    ]);

    const unsubscribeResponsePromise = page.waitForResponse(
      "**/rpc/IRPCStore/unsubscribe"
    );
    await page.getByRole("button", { name: "Unsubscribe" }).click();

    const unsubscribeResponse = await unsubscribeResponsePromise;
    expect(unsubscribeResponse.ok()).toStrictEqual(true);
    expect(unsubscribeResponse.request().postDataJSON()).toStrictEqual([
      subscribeEmail,
    ]);
  });
});
