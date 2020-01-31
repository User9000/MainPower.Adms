# Mainpower.Adms

A suite of tools for migrating data from MainPower systems into The OSI Monarch ADMS

## Enricher

IDF preprocessor that can add, remove and otherwise manipulate the IDF as required.

## EPA Fixer

Manipulates the Enhanced Protocol Analyzer file so that the point indexes reflect the IO addresses instead of the FEP addresses

## Historian Extractor

Extracts historical data from the historian into CSV files, one csv file per point.

## IDF Cleaner

Takes an IDF import and removes the content of every group.  Useful for removing all content from the emap databases and display files without completely hosing the databases allowing content that wasn't imported via IDFto remain.

## IDF Manager

A GUI to manage IDF imports.

## Leika2ADMS

Converts Leika conductor reports into a IDF file.

## ScadaConverter

A tool that reads a Wonderware InTouch database dump and SCD5200 RTU configuration files and morphs it into a series of CSV files with consistent point names, cross-referenced to the original intouch and rtu tag names.
