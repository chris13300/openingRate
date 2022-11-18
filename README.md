# openingRate
Tool to get the book moves rate of a polyglot BIN book on an opening<p>

Prerequisites :<br>
rename BUREAU.ini to YOUR-COMPUTER-NAME.ini<br>
set moteurEPD to path_to_your_engine.exe<br>
set livreBIN to path_to_your_polyglot_book.bin<br>
command : openingRate.exe path_to_your_opening_list.pgn<p>

There are 2 ways to use this tool :<br>
- either run this command : expRate.exe path_to_your_opening_list.pgn<br>
- either run expRate.exe then enter your opening (UCI string)<p>

Normal or Reverse mode :<br>
- normal : the program starts from the opening. It searches for all the book moves until the [80th ply](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L256). It caps out at 10k book moves. The goal is to estimate the book moves rate on an opening.<br>
- reverse : same as normal mode but it caps out at [100 moves](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L243). If the [moves rate is lower than 100](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L338), maybe it lacks of book moves before the last move of the opening so the program restarts from the penultimate move of the opening. The goal is to find where it really begins to lack of book moves on an opening.<br>
