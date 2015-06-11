Desura Collection Dumper
========================

This program takes your Desura collection and exports a manifest of what you have, with keys and download links.

This program maintains several databases:
  - Main database: contains a list of all your games and data needed to export to the other types of databases.
  - Downloads database: a list of games that can be downloaded. Used for generating links.
  - Keys database: a list of keys you have.
  - CDN links file: a plain text list of download links that you can feed into your favorite download manager for quick archiving of game downloads
  - Tokens file: contains cookies that authenticate you with Desura's servers

All databases are in easily readable YAML format.

The major feature of this program is exporting CDN download links. By modifying the downloads database, you can choose which games to generate links for.
This makes it easy to reobtain download links after they expire.

For the keys database, please note that for keys not revealed, the program will not automatically do it for you.
You must follow the links given and reveal them yourselves, then create a fresh copy of the main database. Search for
"Reveal key at http://www.desura.com/games" in your keys database.

If you're getting a database with no downloads or keys, Desura's servers may have crashed and flushed all sessions.
In this case, delete the tokens file (tokens.txt by default) and log in again.

Run program without arguments to download collection and create main, downloads, and keys databases.
Run program with '/xl' and (if not using default names) '/d' or '/i' to generate CDN links.
For other usage scenarios, see help by using '/?' as the argument.
