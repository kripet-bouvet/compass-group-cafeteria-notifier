# Cafeteria Notifier

When run, this application shows a warning if the balance on your card is low. The application works with cards registered with Compass Group/alreadyordered.no

It is called with three arguments: `phone`, `token` and `balanceLimit`. These arguments are explained in the arguments section.

> [!Tip]
> I have set up the notifier to run every weekday at 10:00 using Windows Task Scheduler. This gives me time to top up the balance in time for lunch.

## Building from source

Simply run `dotnet publish` in the directory with the solution file.

## Arguments

To obtain the arguments `phone` and `token`, follow these steps:

1. Visit https://www.alreadyordered.no/compass9002/content/uncode-lite_child/mobile_view.php
2. Log in
3. Open developer tools, and go to the network tab
4. Refresh the page
5. Find the request to `TemplateProductTable_find_current_top_up_value.php`
6. Copy `phone` and `token` from the payload.

> [!NOTE]
> The `phone` argument doesn't seem to be a phone number. For me it's my email address.

> [!NOTE]
> The `token` does not seem to ever expire


The `balanceLimit` argument is the limit where you want to start receiving notifications.

### Example

`CafeteriaNotifier.exe dummy@domain.no dummyToken 200`

This will check if the balance is less than 200 kr, and show a notification if this is the case.
