# Mainpower.Osi

A suite of tools for migrating data into OSI ADMS

## ScadaConverter

A tool that reads a Wonderware InTouch database dump and SCD5200 RTU configuration files and morphs it into a series of CSV files with consistent point names, crossreferenced to the original intouch and rtu tag names.

## Enricher

IDF preprocessor that can add, remove and otherwise manipulate the IDF as required.

## EPA Fixer

Manipulates the Enhanced Protocol Analyzer file so that the point indexes reflect the IO addresses instead of the FEP addresses

## Historian Extractor

Extracts historical data from the historian into CSV files, one csv file per point.
