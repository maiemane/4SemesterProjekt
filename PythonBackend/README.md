# Hedging Engine

Python-delen er beregningsmotoren i platformen. Den skal ikke eje databasen,
API'et eller web-laget. Den skal hente historiske markedsdata, beregne returns,
optimere hedge-vaegte og returnere et auditerbart resultat.

## Ansvar

- Hente fondens NAV-return som target-serie.
- Hente hedge-kandidaters pris-return som forklarende variable.
- Align'e datoer og validere datakvalitet.
- Optimere hedge-vaegte under constraints.
- Beregne beta, R2, korrelation, residualer og tracking error.
- Returnere input, periode, metode og resultater, saa beregningen kan forklares.

## Dokumentation

Dokumentationen er skrevet, saa den kan bruges direkte som grundlag til
rapport og mundtlig forklaring:

- `docs/architecture.md`: overordnet arkitektur og teori.
- `docs/module-walkthrough.md`: forklaring af hver mappe og fil.
- `docs/hedge-math.md`: matematikken bag hedge-modellen.
- `docs/data-processing.md`: databehandling, tidsserier og datakvalitet.
- `docs/design-decisions.md`: beslutninger og argumenter til rapporten.

## Lagdeling

```text
presentation/cli
    -> application/use_cases
        -> application/ports
            <- infrastructure/repositories
            <- infrastructure/optimizers
        -> domain
```

Application-laget kender kun til interfaces/ports. Infrastructure-laget
implementerer adgang til SQL Server og optimeringsbiblioteker.

## Foerste scope

Foerste version af motoren er ikke en komplet produktionsmodel. Den er et
arkitekturskelet, som viser:

- hvordan Excel-logikken kan oversaettes til en use case
- hvordan dataadgang isoleres bag repository interfaces
- hvordan optimeringsmetoden isoleres bag et optimizer interface
- hvordan resultatet kan goeres testbart og auditerbart

## Lokal kørsel

Installer dependencies:

```bash
cd /Users/ac/Documents/hedging4sem/PythonBackend
./.venv/bin/python -m pip install -e '.[dev]'
```

Kør tests:

```bash
./.venv/bin/python -m pytest
```

Kør database smoke-test:

```bash
export HEDGING_DB_CONNECTION_STRING='DIN_CONNECTION_STRING_HER'
PYTHONPATH=src ./.venv/bin/python -m hedging_engine.presentation.cli.smoke_test_database
```

`PYTHONPATH=src` fortæller Python, at source-folderen skal bruges som import
root, saa pakken `hedging_engine` kan findes under lokal udvikling.
