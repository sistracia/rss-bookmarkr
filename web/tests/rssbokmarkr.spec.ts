import { test, expect } from "@playwright/test";
import { RSSBokmarkrPage } from "./rssbokmarkr-page";

const pageURL = process.env.PAGE_URL || "http://localhost:8080";

const generateRandomString = () => {
  return (Math.random() + 1).toString(36).substring(2);
};

const generateUsername = () => {
  return `${generateRandomString()}username`;
};

const generatePassword = () => {
  return `${generateRandomString()}password`;
};

const subscribeEmail = "test@test.com";

const rssURLS = [
  "https://overreacted.io/rss.xml",
  "https://feed.infoq.com/",
  "https://stackoverflow.blog/feed/",
];
const [overreactedURL, infoqURL, stackoverflowURL] = rssURLS;

test("has title", async ({ page }) => {
  const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
  await rssBookmarkrPage.goto();

  await expect(page).toHaveTitle("RSS Bookmarkr");
});

test.describe("single url", () => {
  test("add new url", async ({ page }) => {
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
    await rssBookmarkrPage.goto();

    await rssBookmarkrPage.addURL(overreactedURL);

    await rssBookmarkrPage.expectHaveURLParam([overreactedURL]);
    await expect(page.getByText(overreactedURL)).toBeVisible();
  });

  test("init url with query param", async ({ page }) => {
    const urlParams = [overreactedURL];
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);

    await rssBookmarkrPage.goto(urlParams);

    await rssBookmarkrPage.expectHaveURLParam(urlParams);
    await expect(page.getByText(overreactedURL)).toBeVisible();
  });

  test("delete url", async ({ page }) => {
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
    await rssBookmarkrPage.goto([overreactedURL]);

    await rssBookmarkrPage.deleteURL();

    await rssBookmarkrPage.expectNotHaveURLParam();
  });
});

test.describe("multiple url", () => {
  test("add new url", async ({ page }) => {
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
    await rssBookmarkrPage.goto();

    for (const rssURL of rssURLS) {
      await rssBookmarkrPage.addURL(rssURL);

      await expect(page.getByText(rssURL)).toBeVisible();
    }

    await rssBookmarkrPage.expectHaveURLParam(rssURLS);
  });

  test("init url with query param", async ({ page }) => {
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
    await rssBookmarkrPage.goto(rssURLS);

    for (const rssURL of rssURLS) {
      await expect(page.getByText(rssURL)).toBeVisible();
    }

    await rssBookmarkrPage.expectHaveURLParam(rssURLS);
  });

  test("delete url", async ({ page }) => {
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
    await rssBookmarkrPage.goto(rssURLS);

    await rssBookmarkrPage.deleteURL();

    await rssBookmarkrPage.expectHaveURLParam([infoqURL, stackoverflowURL]);
    await expect(page.getByText(infoqURL)).toBeVisible();
    await expect(page.getByText(stackoverflowURL)).toBeVisible();
  });
});

test.describe("user authentication", () => {
  test("login then logout", async ({ page }) => {
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
    await rssBookmarkrPage.goto();

    await rssBookmarkrPage.login(generateUsername(), generatePassword());
    await rssBookmarkrPage.logout();
  });
});

test.describe("authorized user", () => {
  test("save urls and delete the url", async ({ page }) => {
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
    await rssBookmarkrPage.goto();
    await rssBookmarkrPage.login(generateUsername(), generatePassword());

    await rssBookmarkrPage.addURL(overreactedURL);
    await rssBookmarkrPage.saveURLs([overreactedURL]);

    await rssBookmarkrPage.deleteURL();
    await rssBookmarkrPage.saveURLs([]);
  });

  test("subscribe and unsubscribe", async ({ page }) => {
    const rssBookmarkrPage = new RSSBokmarkrPage(pageURL, page);
    await rssBookmarkrPage.goto();
    await rssBookmarkrPage.login(generateUsername(), generatePassword());

    await rssBookmarkrPage.subscribe(subscribeEmail);
    await rssBookmarkrPage.unsubscribe(subscribeEmail);
  });
});
