# openingRate
Tool to get the book's moves rate of a polyglot BIN book on an opening<p>

Prerequisites :<br>
rename BUREAU.ini to YOUR-COMPUTER-NAME.ini<br>
set moteurEPD to path_to_your_engine.exe<br>
set livreBIN to path_to_your_polyglot_book.bin<br>
copy [pg_query.exe](https://github.com/chris13300/openingRate/blob/main/openingRate/bin/Debug/pg_query.exe) in your openingRate folder<br>
copy [pgn-extract.exe](https://github.com/chris13300/openingRate/blob/main/openingRate/bin/Debug/pgn-extract.exe) in your openingRate folder<br>
some users may need to copy [msvcp120d.dll](https://github.com/chris13300/openingRate/blob/main/openingRate/bin/Debug/msvcp120d.dll) and [msvcr120d.dll](https://github.com/chris13300/openingRate/blob/main/openingRate/bin/Debug/msvcr120d.dll) in their SysWOW64 folder.<br>
command : openingRate.exe path_to_your_opening_list.pgn<p>

There are 2 ways to use this tool :<br>
- either run this command : expRate.exe path_to_your_opening_list.pgn<br>
- either run expRate.exe then enter your opening (UCI string)<p>

Normal or Reverse mode :<br>
- normal : the program starts from the opening. It searches for all the book's moves until the [80th ply](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L256). It caps out at 10k book's moves. The goal is to estimate the book's moves rate on an opening.<br>
- reverse : same as normal mode but it caps out at [100 book's moves](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L325). If the [book's moves rate is lower than 100](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L338), maybe it lacks of book's moves before the last move of the opening so the program restarts from the penultimate move of the opening. The goal is to find where it really begins to lack of book's moves on an opening.<br>
