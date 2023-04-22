Autor: xfudor00
Projekt: Varianta EPSILON: Simple File Transfer Protocol
--------------------------------------------------------
Popis:
- Implementovanie Simple File Transfer Protocolu v jazyku C#
- Použitá asynchrónna komunikácia
  - Je možné spustiť viac klientov, každý má vlastnú inštanciu a dostáva len správy, ktoré mu patria
- Použité externé knižnice:
	- CommandLine : knižnica, ktorá spracúva argumenty
- Projekt je kompatibilný s .NET Core 3.1
- Projekt sa prekladá pomocou Makefile
- Klient posiela správy na server, server odpovedá
- Správy ktoré klient posiela sú na strane serveru interpretované v súlade s RFC 913 - Simple File Transfer Protocol
- Plne funkčné príkazy SFTP:
	- USER, ACCT, PASS, LIST, CDIR, KILL, NAME, DONE
- Neimplementované príkazy SFTP:
	- TYPE, RETR, STOR
---------------------------------------------------------------------------------------------------------------------
Preklad:
- make clean   --> odstráni staré súbory
- make restore --> získa použité nugetové balíčky
- make build   --> preloží klienta aj server
- make publish --> vygeneruje binárne súbory do root-a
- make all     --> odstráni staré súbory, preloží klienta aj server, získa použité nugetové balíčky, vygeneruje binárne súbory do root-a
----------------------------------------------------------------------------------------------------------------------------------------
Príklady spustenia:

 1.
  - Server:
	../xfudor00:~$ ./ipk-simpleftp-server -i eth0 -p 115 -u /home/dir/userpass.tx -f /home/dir

  - Klient:
	../xfudor00:~$ ./ipk-simpleftp-client -h 192.168.0.100 -f /home/dir
--------------------------------------------------------------------------------------------------
 2.
  - Server:
	../xfudor00:~$ ./ipk-simpleftp-server -u /home/dir/userpass.tx -f /home/dir

  - Klient:
	../xfudor00:~$ ./ipk-simpleftp-client -h 192.168.0.100 -f /home/dir
