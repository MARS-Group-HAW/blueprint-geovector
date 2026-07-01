FROM gboeing/osmnx:latest

# 1. Alle GIS-Werkzeuge inklusive Fiona via Mamba installieren
RUN mamba install --yes -c conda-forge \
    gcc \
    unzip \
    setuptools \
    wheel \
    pyogrio \
    geopandas \
    backports.tarfile \
    jupyter_packaging \
    sqlite \
    fiona

# 2. Keplergl ohne Isolation installieren
RUN pip install keplergl --no-build-isolation

# 3. Umgebungsvariable für PROJ
ENV PROJ_DATA=/opt/conda/share/proj

