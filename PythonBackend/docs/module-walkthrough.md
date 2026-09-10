# Module Walkthrough

Dette dokument forklarer, hvordan Python-projektets mapper og filer haenger
sammen. Det kan bruges til at forklare projektet i rapporten eller til eksamen.

## Overblik

```text
PythonBackend
|-- src/hedging_engine
|   |-- domain
|   |-- application
|   |   |-- ports
|   |   `-- use_cases
|   |-- infrastructure
|   |   |-- repositories
|   |   `-- optimizers
|   `-- presentation
|       `-- cli
`-- tests
```

Strukturen er lavet efter Clean Architecture. Det betyder, at de vigtigste
forretningsregler ligger inderst, mens tekniske detaljer ligger yderst.

## domain

`domain` indeholder begreber fra forretningen. I vores projekt er det fx:

- `DateRange`
- `HedgeCandidate`
- `HedgeOptimizationRequest`
- `HedgeWeight`
- `HedgeMetrics`
- `HedgeOptimizationResult`

Domain-laget skal vaere saa uafhaengigt som muligt. Det skal ikke kende SQL,
Pandas, SciPy, API'er eller filer.

Eksempel:

```python
HedgeCandidate(ticker="QQQ", min_weight=-15.0, max_weight=15.0)
```

Det beskriver et hedge-instrument og dets constraints. Det er et
forretningsbegreb, ikke en teknisk database-række.

## application

`application` indeholder de konkrete use cases. En use case beskriver noget,
systemet skal kunne.

I første version har vi:

```text
OptimizeHedgeUseCase
```

Den svarer til platformens vigtigste handling:

```text
Find den bedste hedge for en fond i en given periode.
```

Use casen styrer flowet:

1. Hent fondens NAV-niveauer.
2. Hent hedge-kandidaternes prisniveauer.
3. Beregn returns.
4. Align datoer.
5. Kør optimering.
6. Beregn kvalitetsmål.
7. Returner et samlet resultat.

Application-laget må gerne kende domain-modeller og interfaces. Det bør ikke
kende den konkrete database eller den konkrete tekniske optimeringsadapter.

## application/ports

`ports` er interfaces, som application-laget definerer.

Vi har to vigtige ports:

```text
MarketDataRepository
HedgeOptimizer
```

`MarketDataRepository` siger, hvad motoren skal kunne hente:

```text
load_fund_nav_returns
load_hedge_returns
```

`HedgeOptimizer` siger, hvad motoren skal kunne beregne:

```text
optimize
```

Det er Dependency Inversion Principle i praksis. Application-laget ejer
kontrakten, og infrastructure-laget leverer implementationen.

## application/services

`services` indeholder mindre application-services, som hjælper use cases.

Første service er:

```text
TimeSeriesProcessor
```

Den har ansvar for:

- missing data policy
- return-beregning
- dato-alignment

Det er lagt i application-laget, fordi det er en del af beregningsmodellen.
Hvis vi lagde return-beregningen direkte i SQL repository, ville den finansielle
model blive blandet sammen med databaseadgang.

## infrastructure

`infrastructure` indeholder tekniske detaljer.

Eksempler:

- SQL Server adapter
- Pandas-baseret dataloading
- SciPy-baseret optimering

Det er bevidst, at `SqlMarketDataRepository` ligger her. SQL er en teknisk
detalje, ikke en del af selve hedge-reglen.

Hvis vi senere vil teste modellen uden database, kan vi lave:

```text
InMemoryMarketDataRepository
```

uden at ændre `OptimizeHedgeUseCase`.

## infrastructure/repositories

Repository-klasser implementerer dataadgang.

I vores arkitektur betyder det:

```text
SQL Server tabeller -> price/NAV levels -> application use case
```

Repository-laget skal oversaette fra databaseverdenen til beregningsverdenen.
Det er her, SQL queries senere skal ligge.

## infrastructure/optimizers

Optimizer-klasser implementerer selve optimeringsmetoden.

I første version har vi en SciPy-adapter:

```text
ScipyLeastSquaresOptimizer
```

Den minder om Excel Solver, fordi den kan minimere en objektivfunktion under
constraints.

Excel:

```text
Solver minimerer SUM((NAV return - hedge return)^2)
```

Python:

```text
scipy.optimize.minimize minimerer samme type fejl
```

## presentation

`presentation` er indgangen til motoren. Det kan være:

- CLI worker
- senere et job-runner script
- eventuelt en API-adapter, hvis motoren engang skal kaldes som service

Presentation-laget skal ikke indeholde finansiel logik. Det skal kun parse
input, kalde use casen og vise/skrive output.

Første konkrete database-smoke-test ligger her:

```text
presentation/cli/smoke_test_database.py
```

Den læser connection string fra `HEDGING_DB_CONNECTION_STRING`, kalder SQL
repository og printer kun antal rækker og dato-ranges. Den ændrer ikke data i
databasen.

Hvis hedge-kandidaterne ikke matcher nogen `DAILY_PRICE`-rækker, printer den
eksempler på tickers fra databasen. Det gør smoke-testen nyttig til at finde
ud af, om problemet er ticker-format eller manglende data.

Lokal kørsel fra `PythonBackend`:

```bash
export HEDGING_DB_CONNECTION_STRING='DIN_CONNECTION_STRING_HER'
PYTHONPATH=src ./.venv/bin/python -m hedging_engine.presentation.cli.smoke_test_database
```

## tests

`tests` indeholder automatiske tests.

Første tests validerer domain-regler:

- en slutdato maa ikke komme før startdato
- et hedge-instrument skal have ticker
- en optimeringsrequest skal have mindst en kandidat

Det virker simpelt, men det viser en vigtig pointe: forretningsregler kan
testes uden database, Excel eller UI.
