# Debugging – Logiskt fel i sökfunktionen

**Undersökning av felet**

Applikationen har ett fel där sökfunktionen är skiftlägeskänslig, trots att den inte ska vara det. Om det exempelvis finns ett kundnamn sparat som "Tomas" ska det gå att hitta bokningen genom att söka på "tomas" eller delar av namnet.

# Återskapa felet

För att återskapa felet börjar vi med att skapa en bokning med kundnamnet "Johan". Därefter försöker vi hitta bokningen genom att söka på "johan" med små bokstäver.

Sökningen returnerar inga resultat trots att bokningen finns sparad.

<img width="551" height="471" alt="{3837B0AB-0FB0-4BC2-B8E0-012E498348D6}" src="https://github.com/user-attachments/assets/2bc2bb91-bb11-43b7-a6cd-1e4086a33018" />

# Resultat

Vi dubbelkollar detta genom den inbyggda testen i consolapplikationen:

<img width="583" height="217" alt="{92F5ABB2-5374-411D-ADD5-A26E4524BDF0}" src="https://github.com/user-attachments/assets/d89a13ee-482a-4573-a1ca-cfbc8f3771e7" />

Och om man kollar vid test explorer så ser man att testet misslyckas vid "Assert.Single":

<img width="2114" height="295" alt="{88522680-64C2-4AC1-93A9-B12739D4CF5E}" src="https://github.com/user-attachments/assets/1b88f7db-04d2-4cfd-9f91-c11cf33f2906" />

# Resultat som som blev

Alltså misslyckades testet med felmeddelandet : Assert.Single() Failure: The collection was empty vilket då innebär att
sökningen returnerar en tom lista när det faktiskt finns en bokning på namnet som söktes.

# Vad som borde ha returnerats

Testet ska ha blivit godkänt eftersom bokning med respektive kundnamn finns i systemet, 
då applikationens sökfunktion inte ska vara skriftkänslig, alltså att man ska kunna skriva med
stora eller små bokstäver, men också delar av namnet.

# Felsökning och orsak

Vi kontrollerar metoden som tar hand om search funktionen "public IReadOnlyList<Booking> Search(string searchTerm)" sätter breakpoints för att se
vad som händer:

<img width="746" height="240" alt="{EF50B658-B8CB-41F9-BE85-1A4A4D764145}" src="https://github.com/user-attachments/assets/903be622-cc6d-4d23-952c-60722ab1fca0" />

Koden visar att "searchTerm" är korrekt med "avan".

Men när den har gått igenom hela listan så visar den att det finns inget som passar in på "searchTerm":

<img width="703" height="223" alt="{BE60F10C-B594-4062-94A8-8B14B45A4BED}" src="https://github.com/user-attachments/assets/6b130e81-23d4-43ce-85dc-e0af14ea562a" />

<img width="209" height="58" alt="{673AA208-7D98-4511-BF47-E16AD888FFFE}" src="https://github.com/user-attachments/assets/e8d62f51-2146-4c45-82f0-1df5537ecbee" />

Vilket gör att vi får felmeddelandet.



Koden som orsakar detta problem ligger i sökmetoden inom BookingService:

<img width="640" height="283" alt="{7942C13A-73D2-4233-9F10-32D073EFEB55}" src="https://github.com/user-attachments/assets/692ff5d7-4d5f-41cd-b57a-1f3e953d2aa9" />

Koden ger ett felaktigt resultat för att metoden inte är fullständig, där den just nu anropar GetAll() metoden
för att få tillgång till de sparade bokningar, sedan används LINQ .Where för att gå igenom alla bokningar,
sedan har den två villkor där bokningarna inte får vara avbokade och kontrollerar att kundnamnet innehåller hela eller
delar av söktermen med .Contains. 

Problemet är att Contains() utan StringComparison gör en skiftlägeskänslig jämförelse.

Det innebär exempelvis att "avan" inte matchar "Avanoria AB", eftersom stora och små bokstäver behandlas som olika tecken. Därför returnerar metoden en tom lista trots att bokningen finns.

<img width="755" height="137" alt="{8240B330-FE38-4057-ACBC-0A2E20DE3B71}" src="https://github.com/user-attachments/assets/7dfe40a0-9e1a-4842-b9fc-3122f5ec8c62" />

Vi lägger till StringComparison.OrdinalIgnoreCase i Contains(). och kör igång testet igen.

<img width="2087" height="362" alt="{527F6271-C5B3-4911-AAD5-15BA6C8318E6}" src="https://github.com/user-attachments/assets/f56dc92d-2b73-4618-9d7d-e777fb09d7d3" />

<img width="604" height="211" alt="{6ED2D5FE-3906-4B06-B299-9A45E65FFEE2}" src="https://github.com/user-attachments/assets/d4f6004c-849a-4699-aee4-3bcd891fd17e" />

Vi ser nu att det automatiska testet går igenom, men vi gör även en manuell kontroll i consoleapplikationen:

<img width="669" height="799" alt="{16D95253-CACD-4D03-BA61-493A516A44ED}" src="https://github.com/user-attachments/assets/0d6f44aa-89fb-4cf0-9dc2-93ac01db0a7d" />

Även det manuella testet fungerar nu med att söka med stora eller små bokstäver, hela eller delar av namnet.

# Alternativ lösning

Ett annat alternativ är att använda sig av ToLower(); eller ToLowerInvariant(); på både namn och söktermen för att sökfunktionen inte ska vara skriftkänslig. Men med StringComparison.OrdinalIgnoreCase så görs jämförelsen direkt utan att behöva skapa extra strängar, alltså blir det lättare att läsa, förstå och felsöka.

# Slutsats

Felet berodde på att .Contains() inte hade någon inställning för att ignorera stora och små bokstäver.

Genom att lägga till StringComparison.OrdinalIgnoreCase kunde vi lösa problemet utan att behöva ändra resten av sökmetoden.

Både det automatiska och manuella testet fungerar nu som tänkt.














