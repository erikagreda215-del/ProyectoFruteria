"""
API REST de la Frutería - Proyecto M7
Autor: Erik Agreda (EA)

Arranque:
    pip install -r requirements.txt
    uvicorn main:app --reload
Documentación interactiva: http://127.0.0.1:8000/docs
"""
import json
import threading
from collections import defaultdict
from datetime import datetime, date
from pathlib import Path
from typing import List, Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

# ---------------------------------------------------------------------------
# Persistencia en fichero JSON
# ---------------------------------------------------------------------------
RUTA_DATOS = Path(__file__).parent / "datos.json"
_lock = threading.Lock()

PRODUCTOS_INICIALES = [
    {"id": 1, "nombre": "Manzana Golden", "categoria": "Fruta", "precioKg": 2.20, "stockKg": 40.0},
    {"id": 2, "nombre": "Plátano de Canarias", "categoria": "Fruta", "precioKg": 2.65, "stockKg": 25.0},
    {"id": 3, "nombre": "Naranja", "categoria": "Fruta", "precioKg": 1.60, "stockKg": 60.0},
    {"id": 4, "nombre": "Fresa", "categoria": "Fruta", "precioKg": 4.90, "stockKg": 4.0},
    {"id": 5, "nombre": "Tomate pera", "categoria": "Verdura", "precioKg": 2.10, "stockKg": 30.0},
    {"id": 6, "nombre": "Lechuga", "categoria": "Verdura", "precioKg": 1.80, "stockKg": 3.0},
    {"id": 7, "nombre": "Patata", "categoria": "Hortaliza", "precioKg": 1.10, "stockKg": 80.0},
    {"id": 8, "nombre": "Cebolla", "categoria": "Hortaliza", "precioKg": 1.30, "stockKg": 7.5},
    {"id": 9, "nombre": "Almendra", "categoria": "Fruto seco", "precioKg": 12.50, "stockKg": 6.0},
]


def cargar_datos() -> dict:
    if RUTA_DATOS.exists():
        with open(RUTA_DATOS, "r", encoding="utf-8") as f:
            return json.load(f)
    datos = {"productos": PRODUCTOS_INICIALES, "ventas": []}
    guardar_datos(datos)
    return datos


def guardar_datos(datos: dict) -> None:
    with open(RUTA_DATOS, "w", encoding="utf-8") as f:
        json.dump(datos, f, ensure_ascii=False, indent=2)


# ---------------------------------------------------------------------------
# Modelos (Pydantic)
# ---------------------------------------------------------------------------
class ProductoEntrada(BaseModel):
    """Datos que envía el cliente al crear/editar (el id lo gestiona la API)."""
    nombre: str = Field(min_length=1)
    categoria: str = Field(min_length=1)
    precioKg: float = Field(gt=0)
    stockKg: float = Field(ge=0)


class Producto(ProductoEntrada):
    id: int


class LineaVentaEntrada(BaseModel):
    productoId: int
    kilos: float = Field(gt=0)


class VentaEntrada(BaseModel):
    lineas: List[LineaVentaEntrada] = Field(min_length=1)


class LineaVenta(BaseModel):
    productoId: int
    nombre: str
    kilos: float
    precioKg: float
    subtotal: float


class Venta(BaseModel):
    id: int
    fecha: str  # ISO 8601
    lineas: List[LineaVenta]
    total: float


class DetalleInforme(BaseModel):
    nombre: str
    kilos: float
    importe: float


class InformeVentasHoy(BaseModel):
    fecha: str
    totalVendido: float
    numeroVentas: int
    productoMasVendido: Optional[str]
    kilosProductoMasVendido: float
    detalle: List[DetalleInforme]


# ---------------------------------------------------------------------------
# Aplicación
# ---------------------------------------------------------------------------
app = FastAPI(title="API Frutería", version="1.0", description="Proyecto M7 - Erik Agreda")
datos = cargar_datos()


def _buscar_producto(id: int) -> dict:
    for p in datos["productos"]:
        if p["id"] == id:
            return p
    raise HTTPException(status_code=404, detail=f"Producto {id} no encontrado")


@app.get("/")
def raiz():
    return {"mensaje": "API Frutería funcionando", "docs": "/docs"}


# ----------------------------- PRODUCTOS -----------------------------------
@app.get("/productos", response_model=List[Producto])
def listar_productos():
    return datos["productos"]


@app.get("/productos/{id}", response_model=Producto)
def obtener_producto(id: int):
    return _buscar_producto(id)


@app.post("/productos", response_model=Producto, status_code=201)
def crear_producto(entrada: ProductoEntrada):
    with _lock:
        nuevo_id = max((p["id"] for p in datos["productos"]), default=0) + 1
        producto = {"id": nuevo_id, **entrada.model_dump()}
        datos["productos"].append(producto)
        guardar_datos(datos)
    return producto


@app.put("/productos/{id}", response_model=Producto)
def actualizar_producto(id: int, entrada: ProductoEntrada):
    with _lock:
        producto = _buscar_producto(id)
        producto.update(entrada.model_dump())
        guardar_datos(datos)
    return producto


@app.delete("/productos/{id}")
def eliminar_producto(id: int):
    with _lock:
        producto = _buscar_producto(id)
        datos["productos"].remove(producto)
        guardar_datos(datos)
    return {"ok": True, "eliminado": id}


# ------------------------------- VENTAS ------------------------------------
@app.get("/ventas", response_model=List[Venta])
def listar_ventas():
    return datos["ventas"]


@app.post("/ventas", response_model=Venta, status_code=201)
def registrar_venta(entrada: VentaEntrada):
    with _lock:
        # 1) Agrupar kilos por producto (por si una línea se repite)
        kilos_por_producto = defaultdict(float)
        for linea in entrada.lineas:
            kilos_por_producto[linea.productoId] += linea.kilos

        # 2) Validar TODO antes de tocar el stock (la venta es atómica)
        for pid, kilos in kilos_por_producto.items():
            p = _buscar_producto(pid)
            if kilos > p["stockKg"] + 1e-9:
                raise HTTPException(
                    status_code=400,
                    detail=f"Stock insuficiente de {p['nombre']}: "
                           f"quedan {p['stockKg']:.2f} kg y se piden {kilos:.2f} kg",
                )

        # 3) Descontar stock y construir la venta
        lineas = []
        for pid, kilos in kilos_por_producto.items():
            p = _buscar_producto(pid)
            p["stockKg"] = round(p["stockKg"] - kilos, 3)
            lineas.append({
                "productoId": pid,
                "nombre": p["nombre"],
                "kilos": round(kilos, 3),
                "precioKg": p["precioKg"],
                "subtotal": round(kilos * p["precioKg"], 2),
            })

        venta = {
            "id": max((v["id"] for v in datos["ventas"]), default=0) + 1,
            "fecha": datetime.now().isoformat(timespec="seconds"),
            "lineas": lineas,
            "total": round(sum(l["subtotal"] for l in lineas), 2),
        }
        datos["ventas"].append(venta)
        guardar_datos(datos)
    return venta


# ------------------------------ INFORMES -----------------------------------
@app.get("/informes/ventas-hoy", response_model=InformeVentasHoy)
def informe_ventas_hoy():
    hoy = date.today().isoformat()
    ventas_hoy = [v for v in datos["ventas"] if v["fecha"].startswith(hoy)]

    kilos = defaultdict(float)
    importe = defaultdict(float)
    for v in ventas_hoy:
        for l in v["lineas"]:
            kilos[l["nombre"]] += l["kilos"]
            importe[l["nombre"]] += l["subtotal"]

    mas_vendido = max(kilos, key=kilos.get) if kilos else None
    detalle = sorted(
        [{"nombre": n, "kilos": round(kilos[n], 3), "importe": round(importe[n], 2)} for n in kilos],
        key=lambda d: d["kilos"], reverse=True,
    )
    return {
        "fecha": hoy,
        "totalVendido": round(sum(v["total"] for v in ventas_hoy), 2),
        "numeroVentas": len(ventas_hoy),
        "productoMasVendido": mas_vendido,
        "kilosProductoMasVendido": round(kilos[mas_vendido], 3) if mas_vendido else 0.0,
        "detalle": detalle,
    }
