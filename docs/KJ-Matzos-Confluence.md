---
title: KJ Matzos — In-Store Order Lookup (Proposal)
confluence-page-id: 164167681
confluence-space: KJMATZ
---

# KJ Matzos — In-Store Order Lookup

**Prepared for:** KJ Matzos  
**Prepared by:** RDT Systems  
**Date:** June 2026  
**Status:** Proposal — development in progress

---

## At a glance

We are building a **tablet kiosk** for your store so customers can **look up their own matzah order history** by **phone number** — the same idea as your **self-checkout / ItemSale** screens, but **read-only**:

- **No payment** on the tablet  
- **No prices** — only **items and pounds**  
- **Print** a visit receipt or a **year total** on your receipt printer  
- **Yiddish** on screen, matching your mockups  

Your team will spend less time answering *“What did I take last year?”* while the customer waits.

---

## The problem today

| Today | With the kiosk |
|--------|----------------|
| Customer asks staff to look up history | Customer looks it up on the tablet |
| Staff opens Phone Order or back office | No cashier login needed on the floor |
| Takes staff time during busy season | Self-service while they wait |

---

## What we will deliver

### Screen 1 — Enter phone number (ItemSale 1)

Customer sees a large keypad (like your mockup), with on-screen text:

**ביטע לייגט אריין אייער טעלעפאן נומער צו זעהן אלע אייער אפוינטמענטס און היסטארי**

- Round number buttons and a **green search** button  
- Phone shown in standard US format `(XXX) XXX-XXXX`, with backspace on the keypad  
- After a short idle time, the screen resets for the next customer  

![Screen 1 — Phone entry](item_sale_1.png)

---

### Screen 2 — Profile & year selection (ItemSale 2)

After phone lookup, the customer sees **profile information** and picks a **Hebrew year** (last three years **with purchases**, plus **see more** when needed):

| Element | Detail |
|---------|--------|
| **Header** | אינפארמאציע וואס די סיסטעם האט געפונען פאר די טעלעפאן נומער |
| **Profile** | Name, **full** phone/cell numbers, email, address |
| **Edit** | Optional — may be added later |
| **Years** | **די יארן וואס איר גענומען מצות ביי אונז** — only years with orders (e.g. תשפ״ו, תשפ״ד, תשפ״ג if תשפ״ה had no purchases) |
| **See more years** | When customer has more than three purchase years |
| **New phone** | Top right — new lookup |
| **Back** | Return to phone entry |

![Screen 2 — Profile & years (ItemSale 2)](item_sale_2.png)

---

### Screen 3 — Print receipts & year total (ItemSale 3)

Your **ItemSale 3** mockup: **scroll** through all visit columns for the year, year summary, and print actions.

| Control | Action |
|---------|--------|
| **דרוק** (under each column) | Print that visit — customer info, date, items/pounds only (**Yiddish**, no payment) |
| **דרוק סך הכל אליין** | Print year total by item type — same rules |
| **New phone** | New lookup |
| **Back** | Previous screen |

Each visit shows **Hebrew and English** dates (as in mockup).

![Screen 3 — Print (ItemSale 3)](item_sale_3.png)

---

## Features summary

| Feature | Included |
|---------|----------|
| Tablet-friendly, self-checkout style UI | Yes |
| Phone number lookup | Yes |
| Order history by visit | Yes |
| **Last 3 purchase years** (Hebrew) + see more | Yes |
| Totals in **pounds** by item type | Yes |
| **No money** on screen | Yes |
| Print per visit + print year total | Yes |
| Yiddish screen text (per mockups) | Yes |
| Customer places new orders on kiosk | **No** |
| Edit or cancel orders on kiosk | **No** |
| Login to back office on kiosk | **No** |

Opening a customer’s history from the register (skip phone on the tablet) is **related to the POS** — not part of this tablet lookup app or this proposal.

---

## Confirmed requirements (KJ answers)

| Topic | Decision |
|-------|----------|
| **Visits on Screen 3** | **Scroll** — show all visits for the year (not capped at three). |
| **Print — each visit** | Print **customer info** + **visit date** + **items/pounds only** (same as on screen). **No payment** lines. **Yiddish** on the printed slip. |
| **Print — year total** | Same: customer info + year summary by item/pounds only. **Yiddish**. No payment. |
| **EDIT YOUR INFO** | **Optional** — per-field edit screen (name, email, address, phone, phone 2). |
| **POS shortcut** | **Not this app** — opening history from the register is a **POS** feature, not part of this tablet lookup. |
| **Phone display** | Show **full phone numbers** (not masked). |
| **Lookup** | **Phone and cell** numbers (all numbers linked to the customer). |
| **Data** | Live from database — new customers and updated/deleted transactions appear when looked up. **Inactive customers** are not shown. |
| **Screen 2 — years** | Show the **last three Hebrew years the customer actually bought** (skip empty years). Example: if they did not buy in 5785, show 5786, 5784, 5783. **Button** to see **more than three years** when available. Current year shows when they already bought this season. |
| **Language** | **Yiddish only** on the tablet. |
| **Dates** | **Screen 2:** Hebrew year buttons. **Screen 3:** **Hebrew and English** dates on each visit (as in mockup). |
| **Printers** | Each tablet/computer can be configured to use a **different receipt printer**. |

---

## Privacy & security (summary)

- Kiosk is **read-only** — customers cannot change orders or see payment cards.  
- **No staff password** on the tablet.  
- Screen **clears automatically** after idle time.  
- Data stays **KJ Matzos only** — customers see only their own history.  
- **Inactive customers** are excluded from lookup.

---

## Next step

1. Review this proposal and confirm the **mockups you sent us** are reflected correctly above (Screens 1–3).  

Thank you — we look forward to delivering this for KJ Matzos.
