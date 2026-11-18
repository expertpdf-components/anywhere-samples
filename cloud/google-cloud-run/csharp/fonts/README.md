# Your own fonts

Put the `.ttf` / `.otf` font files your HTML uses (by `font-family` name) in this folder. They are
copied to a `fonts` folder next to the application on build and publish, and ExpertPdf Anywhere uses
them for HTML conversion and for text drawn with the library - on every platform, without installing
anything on the server.

The fonts of the `ExpertPdf.Anywhere.Fonts` package go to `fonts/noto`; this folder is for yours.
Check that each font's license allows you to redistribute it with your application.
