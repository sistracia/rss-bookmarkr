import { test, expect } from "@playwright/test";

const pageURL = process.env.PAGE_URL || "http://localhost:8080"

test("has title", async ({ page }) => {
  await page.goto(pageURL);

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/RSS Bookmarkr/);
});
