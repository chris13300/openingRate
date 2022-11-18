# openingRate
Tool to get the book move rate of a polyglot BIN book on an opening

Prerequisites :
rename BUREAU.ini to YOUR-COMPUTER-NAME.ini
set moteurEPD to path_to_your_engine.exe
set livreBIN to path_to_your_polyglot_book.bin
command : openingRate.exe path_to_your_opening_list.pgn

There are 2 ways to use this tool :<br>
- either run this command : expRate.exe path_to_your_opening_list.pgn<br>
- either run expRate.exe then enter your opening (UCI string)<p>

Normal or Reverse mode :<br>
- normal : the program starts from the opening. It searches for all the book moves until the [80th ply](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L160). It caps out at 1M moves. The goal is to estimate the move rate on an opening.
- reverse : same as normal mode but it caps out at [5k moves](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L243). If the [move rate is lower than 1k](https://github.com/chris13300/openingRate/blob/main/openingRate/modMain.vb#L256), maybe it lacks of book moves before the last move of the opening so the program restarts from the penultimate move of the opening. The goal is to find where it really begins to lack book moves on an opening.
