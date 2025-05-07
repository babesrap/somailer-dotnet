
# WineLovers 
Platforma dla miłośników wina
WineLovers to kompleksowa aplikacja internetowa dedykowana miłośnikom wina, oferująca szeroki zakres funkcjonalności związanych z odkrywaniem, poznawaniem i docenianiem światowej kultury wina. Projekt został zrealizowany w technologii ASP.NET Core MVC z wykorzystaniem Entity Framework Core do komunikacji z bazą danych. Projekt jest obecnie w fazie aktywnego rozwoju. Niektóre funkcjonalności mogą być niedostępne lub działać w ograniczonym zakresie.

## Główne Funkcje
**Encyklopedia regionów winiarskich**

- Przeglądanie regionów winiarskich na całym świecie uporządkowanych według krajów
- Szczegółowe informacje o każdym regionie, w tym liczba win i producentów
- Intuicyjny interfejs z wizualnymi elementami jak flagi krajów i tematyczne zdjęcia tła

**Quiz o winach**

- Interaktywny quiz sprawdzający wiedzę o winach, regionach i szczepach
- Różne poziomy trudności dostosowane do wiedzy użytkownika
- Możliwość porównania wyników z innymi użytkownikami

**AI Sommelier**

- Zaawansowany asystent wykorzystujący sztuczną inteligencję
- Rekomendacje win na podstawie preferencji smakowych
- Sugestie dotyczące łączenia win z potrawami

**Edukacja winiarska**

- Baza wiedzy dla pasjonatów i początkujących
- Artykuły na temat produkcji wina, degustacji i kultury winiarskiej
- Materiały edukacyjne dotyczące różnych szczepów i technik winifikacji

## Technologie

- **Backend** - ASP.NET Core MVC (.NET 8)
- **Baza danych** - PostgreSQL z Entity Framework Core
- **Frontend** - HTML5, CSS3, JavaScript z wykorzystaniem biblioteki Tailwind CSS
- **Autentykacja**  - Wbudowany system uwierzytelniania ASP.NET Identity
- **Hostowanie** - Azure App Service

## Architektura

Projekt wykorzystuje klasyczną architekturę warstwową MVC (Model-View-Controller) z naciskiem na czysty kod i separację odpowiedzialności:

- **Modelsd** - Obiekty domenowe reprezentujące kraje, regiony, winiarnie, wina i użytkowników
- **Views** - Responsywne widoki Razor dostosowane do różnych urządzeń
- **Controllers** - Kontrolery obsługujące logikę biznesową i komunikację z bazą danych

## Cele Projektu

WineLovers to nie tylko katalog win, ale kompletna platforma społecznościowa dla entuzjastów wina. Aplikacja łączy elementy edukacyjne, społecznościowe i rekomendacyjne, aby zapewnić użytkownikom kompleksowe narzędzie do zgłębiania fascynującego świata win. Dzięki intuicyjnemu interfejsowi i bogatej bazie danych, WineLovers sprawia, że odkrywanie nowych win i regionów staje się przyjemnością dostępną dla każdego.
