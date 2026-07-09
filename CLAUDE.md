# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

A starter blueprint for building **agent-based simulations on real-world, georeferenced data** using the [MARS](https://www.mars-group.org/) framework (`Mars.Life.Simulations`, .NET 10). It combines:

- Jupyter notebooks (Python/GeoPandas/OSMnx) that download street-network graphs and Point-of-Interest (POI) data from OpenStreetMap/Geofabrik for a chosen area of interest (AOI), and
- A C# MARS model (`GeoVectorBlueprint/`) that loads that data and simulates agents (`Human`) moving between POIs along the street network.

The two halves are connected only through files on disk (GeoJSON in `GeoVectorBlueprint/Resources/`) — there is no direct code coupling between the Python notebooks and the C# model.

## Repository layout

- `Download Graph.ipynb` — downloads a street network (graph) for an AOI via OSMnx, writes `GeoVectorBlueprint/Resources/edges_<network_type>.geojson`.
- `Prepare POIs.ipynb` — downloads a Geofabrik shapefile/GeoPackage for a region, clips it to the AOI, optionally filters by `fclass` category, writes `GeoVectorBlueprint/Resources/pois.geojson`.
- `Analyze.ipynb` — reads simulation output CSV/GeoJSON from the model's build output directory and visualizes it (bar charts, static route heatmap, interactive kepler.gl map via `map_config.py`).
- `*.wkt` files (e.g. `Hamburg_HAW_Area.wkt`, `Ottawa.wkt`, `Port_Elizabeth.wkt`) — AOI polygons in Well-Known Text, drawn via geojson.io and consumed by the two download notebooks. The active AOI is selected by setting `WKT_FILE` at the top of each notebook.
- `map_config.py` — kepler.gl map configuration dict, loaded by `Analyze.ipynb` via `%run`.
- `GeoVectorBlueprint/` — the .NET/MARS simulation model (see below).
- `Dockerfile` / `notebookdocker.sh` / `notebookdocker.bat` — builds a JupyterLab image (based on `gboeing/osmnx`) with the GIS dependencies needed by the notebooks.

### GeoVectorBlueprint (C# MARS model)

- `Program.cs` — entry point. Registers layer/agent types on a `ModelDescription`, loads `config.json`, and runs the simulation via `SimulationStarter`.
- `config.json` — simulation configuration: sim time window (`globals`), input layer files (`layers`), agent counts and output kinds (`agents`). This is the primary place to tweak run duration, agent count, and output format without touching code.
- `Model/GraphLayer.cs` — wraps a `SpatialGraphEnvironment` built from the edges GeoJSON; spawns `Human` agents onto random graph nodes.
- `Model/PoiLayer.cs` — a `VectorLayer` over `pois.geojson`; exposes `GetRandomPoiForCategory(category)` (matches on the `fclass` attribute, throws if none found).
- `Model/Human.cs` — the agent. On `Init`, drops onto a random graph node and creates a route to a random POI (currently hardcoded to `"restaurant"` in `CreateNewRoute()`). On each `Tick`, advances ~2m/s along its route via `Environment.Move`; on reaching the goal, picks a new random POI and route.
- `Resources/` — the GeoJSON inputs (`edges_drive.geojson`, `pois.geojson`) consumed by the layers above, plus timestamped `bkp_*.geojson` backups automatically created by the notebooks when re-running downloads.

Data flow: `config.json` layer file paths → `GraphLayer`/`PoiLayer.InitLayer` → `Human` agents query `PoiLayer` for destinations and `GraphLayer.Environment` for routing/movement → simulation loop runs for the configured `startPoint`..`endPoint` window → CSV + trip GeoJSON output written to the build output directory (e.g. `bin/Debug/net10.0/Human.csv`, `Human_trips.geojson`).

## Common commands

### Running the simulation model

```sh
cd GeoVectorBlueprint
dotnet run -sm config.json
```

(equivalent to `run.sh`). Alternatively open `GeoVectorBlueprint.sln` in Rider/Visual Studio and run.

Build only:

```sh
dotnet build GeoVectorBlueprint.sln
```

There is no test project in this repository.

### Notebook environment

The notebooks require GIS-heavy Python dependencies (GDAL/Fiona/GeoPandas/OSMnx/keplergl) that are best obtained via the provided Docker image rather than a bare `pip install`:

```sh
./notebookdocker.sh     # macOS/Linux — builds the `notebook` image and runs it with the repo mounted, JupyterLab on http://localhost:8888/
notebookdocker.bat       # Windows equivalent
```

Notebooks must be run in order for a fresh AOI: `Download Graph.ipynb` → `Prepare POIs.ipynb` → run the model → `Analyze.ipynb`.

## Working with an area of interest (AOI)

1. Draw/export a rectangle as WKT (e.g. via geojson.io) and save it as a `.wkt` file at the repo root.
2. In `Download Graph.ipynb`, set `WKT_FILE` and `NETWORK_TYPE`, run all cells — writes `edges_<NETWORK_TYPE>.geojson` into `GeoVectorBlueprint/Resources/` (backing up any existing file first).
3. In `Prepare POIs.ipynb`, set `SHP_FILE_URL` (the Geofabrik download for the containing region), `WKT_FILE`, and optionally `FILTER_CLASSES` — writes `pois.geojson` into `GeoVectorBlueprint/Resources/` (backing up any existing file first).
4. If `config.json`'s `layers[].file` entries don't match the generated filenames (e.g. a non-`drive` `NETWORK_TYPE`), update them to match.
5. Run the model, then use `Analyze.ipynb` (adjust `PATH` to the model's build output directory) to inspect results.

## Notes

- `Human.CreateNewRoute()` hardcodes the POI category `"restaurant"` — change this to target a different `fclass` category present in `pois.geojson`.
- `PoiLayer.GetRandomPoiForCategory` throws `ArgumentException` if the requested category has zero POIs in the loaded data; the AOI/filter must actually contain that category.
- POI categories are OSM `fclass` values; see the Geofabrik POI documentation linked from `Prepare POIs.ipynb` for the full list.
