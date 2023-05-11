# YouTube Bulk Upload UI - Lazy Edition

## IMPORTANT

It seems the Google has removed the application's registration for their own inscrutable reasons. For the moment the app won't work as is and I don't have time to update it. Feel free to rebuild it with your own client-secret.json etc.



A simple tool for uploading multiple videos to your YouTube channel.

I needed something to help me upload my vacation videos and now I'm sharing it with you.

I also had the bad experience of paying for a similar application, which the developers later broke, then demanded I pay for the new version to continue using. Hopefully this will help others avoid this kind of inconvenience.

![app screenshot](https://github.com/staafl/youtube-bulk-upload-ui/blob/main/screenshot.png?raw=true)

## How to Use

Requires .NET 4.6.1. Download the latest release from here: https://github.com/staafl/youtube-bulk-upload-ui/releases/latest

No installation needed. Start the application and you'll be prompted to provide access to your YouTube account in order to upload videos. The login data is stored in the %APPDATA%\YouTube.Auth.Store folder so you can delete it and run the app again to log in with another user.

NB: As of 2021-07-22, Google Cloud API verification is still ongoing for the application and you'll see a "Google hasn't verified this app" screen on the consent page. You'll need to click on "Advanced" and click on the "Go to Youtube Bulk Upload UI (unsafe)" link if you want to use it. Sorry about that, but the validation process has a lot of details that need to be tweaked and a lot of back and forth over email, and I'm busy so the process is progressing slowly. You're welcome to wait a few weeks instead until verification is hopefully officially complete.

After logging in, you can drag videos files to the main grid of the application and edit their details. When you're finished, just hit "Upload" and the app will do its job.

To remove videos from the grid, select the lines on the grid and hit "Delete".

The title and description fields support the following placeholders: %f - file name, %i - order of file on the grid, %c - total number of files on the grid (so having five videos with title "%f - part %i/%c" will upload them with titles like "my video - part 2/5")

During upload, the application writes two files: upload-list.log, which lists the titles of the uploaded videos and their URLs, and upload.csv, which contains more details and can be opened in Excel. Copy these files if you want to keep them for reference. When uploading finishes, the application will automatically open upload-list.log.

This app should work on MacOS or Linux with Mono.

## Planned Features

- Reading video EXIF tags to populate title and description automatically or with a pattern supplied by the user.

- Export / import lists of videos to upload in CSV file format (e.g. for compatibility with Excel).

- Resuming failed uploads.

- Notify user when upload is complete, e.g. via push notification or by running a shell task.

- Integrate with ffmpeg for automatic stabilization etc.

## Privacy Policy

This app doesn't gather or publish ANY data from the user's machine other than to YouTube's servers for the purpose of uploading videos.

This app's use of information received from Google APIs will adhere to the [Google API Services User Data Policy](https://developers.google.com/terms/api-services-user-data-policy#additional_requirements_for_specific_api_scopes), including the Limited Use requirements.

## Building

Download the code here: [https://github.com/staafl/youtube-bulk-upload-ui](https://github.com/staafl/youtube-bulk-upload-ui)

To build the project you'll need to provide your own client_secret.json from Google Cloud, since if I publish the client secret used in the binary, Google will likely revoke it. Protecting the client secret is also the reason why the published release is obfuscated. Just paste the JSON file in a static class ClientSecret with a string constant.

## Donations

If this tool helps you, consider sending a small donation to a charity of your choice and dropping me a line. You'll totally make my day. You can also donate a few bucks through me, in which case 100% of your donation will be forwarded to a children or animal shelter.

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=F7GH776DZEFNU)
