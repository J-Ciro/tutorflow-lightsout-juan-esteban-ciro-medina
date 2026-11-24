const http = require("http");

function responder(res, estado, cuerpo) {
  res.writeHead(estado, { "Content-Type": "application/json; charset=utf-8" });
  res.end(JSON.stringify(cuerpo));
}

function construirRutas(datos) {
  return [
    [/^\/estudiantes\/([^/]+)$/, (id) => datos.estudiantes.find((e) => e.id === id)],
    [/^\/estudiantes\/([^/]+)\/notas$/, (id) => (datos.estudiantes.some((e) => e.id === id) ? datos.notas.filter((n) => n.estudianteId === id) : undefined)],
    [/^\/estudiantes\/([^/]+)\/asistencia$/, (id) => (datos.estudiantes.some((e) => e.id === id) ? datos.asistencia.filter((a) => a.estudianteId === id) : undefined)],
    [/^\/programas\/([^/]+)\/estudiantes$/, (id) => (datos.programas.some((p) => p.id === id) ? datos.estudiantes.filter((e) => e.programaId === id) : undefined)],
    [/^\/coordinadores\/([^/]+)$/, (id) => datos.coordinadores.find((c) => c.id === id)],
    [/^\/programas$/, () => datos.programas],
  ];
}

function crear(datos) {
  const rutas = construirRutas(datos);
  const servidor = http.createServer((req, res) => {
    const url = new URL(req.url, "http://localhost");
    if (req.method !== "GET") {
      return responder(res, 405, { error: "Solo GET" });
    }
    if (url.pathname === "/salud") {
      return responder(res, 200, { estado: "ok", periodo: datos.periodo });
    }
    for (const [expresion, resolver] of rutas) {
      const coincidencia = url.pathname.match(expresion);
      if (coincidencia) {
        const salida = resolver(decodeURIComponent(coincidencia[1] || ""));
        return salida === undefined ? responder(res, 404, { error: "No encontrado" }) : responder(res, 200, salida);
      }
    }
    return responder(res, 404, { error: "Ruta no existe" });
  });
  servidor.listen(process.env.PORT);
}

const original = {
  "institucion": "Corporación Educativa Horizonte (ficticia)",
  "periodo": "2026-2",
  "programas": [
    {
      "id": "ING-SIS",
      "nombre": "Ingeniería de Sistemas",
      "coordinadorId": "COORD-01"
    },
    {
      "id": "ADM",
      "nombre": "Administración de Empresas",
      "coordinadorId": "COORD-02"
    }
  ],
  "coordinadores": [
    {
      "id": "COORD-01",
      "nombre": "Marcela Ríos",
      "programas": [
        "ING-SIS"
      ]
    },
    {
      "id": "COORD-02",
      "nombre": "Julián Pardo",
      "programas": [
        "ADM"
      ]
    }
  ],
  "estudiantes": [
    {
      "id": "EST-001",
      "nombre": "Laura Gómez",
      "programaId": "ING-SIS",
      "semestre": 3
    },
    {
      "id": "EST-002",
      "nombre": "Andrés Martínez",
      "programaId": "ING-SIS",
      "semestre": 3
    },
    {
      "id": "EST-003",
      "nombre": "Sofía Herrera",
      "programaId": "ING-SIS",
      "semestre": 3
    },
    {
      "id": "EST-004",
      "nombre": "Camilo Rojas",
      "programaId": "ADM",
      "semestre": 2
    },
    {
      "id": "EST-005",
      "nombre": "Valentina Castro",
      "programaId": "ADM",
      "semestre": 2
    },
    {
      "id": "EST-006",
      "nombre": "Mateo Vargas",
      "programaId": "ADM",
      "semestre": 2
    }
  ],
  "notas": [
    {
      "estudianteId": "EST-001",
      "codigo": "CAL-201",
      "asignatura": "Cálculo Integral",
      "creditos": 4,
      "corte1": 4.2,
      "corte2": 4.5,
      "corte3": 4.6
    },
    {
      "estudianteId": "EST-001",
      "codigo": "POO-202",
      "asignatura": "Programación Orientada a Objetos",
      "creditos": 3,
      "corte1": 4.8,
      "corte2": 4.6,
      "corte3": 4.9
    },
    {
      "estudianteId": "EST-001",
      "codigo": "BD-203",
      "asignatura": "Bases de Datos",
      "creditos": 3,
      "corte1": 4.0,
      "corte2": 3.8,
      "corte3": 4.4
    },
    {
      "estudianteId": "EST-001",
      "codigo": "ING-204",
      "asignatura": "Inglés III",
      "creditos": 2,
      "corte1": 3.9,
      "corte2": 4.1,
      "corte3": 4.0
    },
    {
      "estudianteId": "EST-002",
      "codigo": "CAL-201",
      "asignatura": "Cálculo Integral",
      "creditos": 4,
      "corte1": 2.5,
      "corte2": 2.8,
      "corte3": 2.6
    },
    {
      "estudianteId": "EST-002",
      "codigo": "POO-202",
      "asignatura": "Programación Orientada a Objetos",
      "creditos": 3,
      "corte1": 3.5,
      "corte2": 3.2,
      "corte3": 3.6
    },
    {
      "estudianteId": "EST-002",
      "codigo": "BD-203",
      "asignatura": "Bases de Datos",
      "creditos": 3,
      "corte1": 3.8,
      "corte2": 3.6,
      "corte3": 3.9
    },
    {
      "estudianteId": "EST-002",
      "codigo": "ING-204",
      "asignatura": "Inglés III",
      "creditos": 2,
      "corte1": 3.0,
      "corte2": 3.4,
      "corte3": 3.1
    },
    {
      "estudianteId": "EST-003",
      "codigo": "CAL-201",
      "asignatura": "Cálculo Integral",
      "creditos": 4,
      "corte1": 4.0,
      "corte2": 3.9,
      "corte3": 4.2
    },
    {
      "estudianteId": "EST-003",
      "codigo": "POO-202",
      "asignatura": "Programación Orientada a Objetos",
      "creditos": 3,
      "corte1": 3.7,
      "corte2": 4.0,
      "corte3": 3.8
    },
    {
      "estudianteId": "EST-003",
      "codigo": "BD-203",
      "asignatura": "Bases de Datos",
      "creditos": 3,
      "corte1": 4.1,
      "corte2": 4.3,
      "corte3": 4.0
    },
    {
      "estudianteId": "EST-003",
      "codigo": "ING-204",
      "asignatura": "Inglés III",
      "creditos": 2,
      "corte1": 4.5,
      "corte2": 4.4,
      "corte3": 4.7
    },
    {
      "estudianteId": "EST-004",
      "codigo": "CON-101",
      "asignatura": "Contabilidad",
      "creditos": 3,
      "corte1": 3.6,
      "corte2": 3.9,
      "corte3": 4.1
    },
    {
      "estudianteId": "EST-004",
      "codigo": "MIC-102",
      "asignatura": "Microeconomía",
      "creditos": 3,
      "corte1": 3.4,
      "corte2": 3.0,
      "corte3": 3.5
    },
    {
      "estudianteId": "EST-004",
      "codigo": "EST-103",
      "asignatura": "Estadística",
      "creditos": 4,
      "corte1": 4.0,
      "corte2": 4.2,
      "corte3": 3.9
    },
    {
      "estudianteId": "EST-004",
      "codigo": "COM-104",
      "asignatura": "Comunicación Oral y Escrita",
      "creditos": 2,
      "corte1": 4.5,
      "corte2": 4.6,
      "corte3": 4.4
    },
    {
      "estudianteId": "EST-005",
      "codigo": "CON-101",
      "asignatura": "Contabilidad",
      "creditos": 3,
      "corte1": 3.2,
      "corte2": 3.0,
      "corte3": 3.3
    },
    {
      "estudianteId": "EST-005",
      "codigo": "MIC-102",
      "asignatura": "Microeconomía",
      "creditos": 3,
      "corte1": 1.9,
      "corte2": 1.6,
      "corte3": 1.9
    },
    {
      "estudianteId": "EST-005",
      "codigo": "EST-103",
      "asignatura": "Estadística",
      "creditos": 4,
      "corte1": 2.4,
      "corte2": 2.2,
      "corte3": 2.3
    },
    {
      "estudianteId": "EST-005",
      "codigo": "COM-104",
      "asignatura": "Comunicación Oral y Escrita",
      "creditos": 2,
      "corte1": 3.8,
      "corte2": 3.5,
      "corte3": 3.9
    },
    {
      "estudianteId": "EST-006",
      "codigo": "CON-101",
      "asignatura": "Contabilidad",
      "creditos": 3,
      "corte1": 4.1,
      "corte2": 4.0,
      "corte3": 4.3
    },
    {
      "estudianteId": "EST-006",
      "codigo": "MIC-102",
      "asignatura": "Microeconomía",
      "creditos": 3,
      "corte1": 3.6,
      "corte2": 3.8,
      "corte3": 3.9
    },
    {
      "estudianteId": "EST-006",
      "codigo": "EST-103",
      "asignatura": "Estadística",
      "creditos": 4,
      "corte1": 3.9,
      "corte2": 4.1,
      "corte3": null
    },
    {
      "estudianteId": "EST-006",
      "codigo": "COM-104",
      "asignatura": "Comunicación Oral y Escrita",
      "creditos": 2,
      "corte1": 4.2,
      "corte2": 4.0,
      "corte3": 4.4
    }
  ],
  "asistencia": [
    {
      "estudianteId": "EST-001",
      "codigo": "CAL-201",
      "asignatura": "Cálculo Integral",
      "clasesProgramadas": 64,
      "inasistencias": 1
    },
    {
      "estudianteId": "EST-001",
      "codigo": "POO-202",
      "asignatura": "Programación Orientada a Objetos",
      "clasesProgramadas": 48,
      "inasistencias": 0
    },
    {
      "estudianteId": "EST-001",
      "codigo": "BD-203",
      "asignatura": "Bases de Datos",
      "clasesProgramadas": 48,
      "inasistencias": 2
    },
    {
      "estudianteId": "EST-001",
      "codigo": "ING-204",
      "asignatura": "Inglés III",
      "clasesProgramadas": 32,
      "inasistencias": 1
    },
    {
      "estudianteId": "EST-002",
      "codigo": "CAL-201",
      "asignatura": "Cálculo Integral",
      "clasesProgramadas": 64,
      "inasistencias": 3
    },
    {
      "estudianteId": "EST-002",
      "codigo": "POO-202",
      "asignatura": "Programación Orientada a Objetos",
      "clasesProgramadas": 48,
      "inasistencias": 2
    },
    {
      "estudianteId": "EST-002",
      "codigo": "BD-203",
      "asignatura": "Bases de Datos",
      "clasesProgramadas": 48,
      "inasistencias": 9
    },
    {
      "estudianteId": "EST-002",
      "codigo": "ING-204",
      "asignatura": "Inglés III",
      "clasesProgramadas": 32,
      "inasistencias": 1
    },
    {
      "estudianteId": "EST-003",
      "codigo": "CAL-201",
      "asignatura": "Cálculo Integral",
      "clasesProgramadas": 64,
      "inasistencias": 2
    },
    {
      "estudianteId": "EST-003",
      "codigo": "POO-202",
      "asignatura": "Programación Orientada a Objetos",
      "clasesProgramadas": 48,
      "inasistencias": 1
    },
    {
      "estudianteId": "EST-003",
      "codigo": "BD-203",
      "asignatura": "Bases de Datos",
      "clasesProgramadas": 48,
      "inasistencias": 0
    },
    {
      "estudianteId": "EST-003",
      "codigo": "ING-204",
      "asignatura": "Inglés III",
      "clasesProgramadas": 32,
      "inasistencias": null
    },
    {
      "estudianteId": "EST-004",
      "codigo": "CON-101",
      "asignatura": "Contabilidad",
      "clasesProgramadas": 48,
      "inasistencias": 0
    },
    {
      "estudianteId": "EST-004",
      "codigo": "MIC-102",
      "asignatura": "Microeconomía",
      "clasesProgramadas": 48,
      "inasistencias": 2
    },
    {
      "estudianteId": "EST-004",
      "codigo": "EST-103",
      "asignatura": "Estadística",
      "clasesProgramadas": 64,
      "inasistencias": 3
    },
    {
      "estudianteId": "EST-004",
      "codigo": "COM-104",
      "asignatura": "Comunicación Oral y Escrita",
      "clasesProgramadas": 32,
      "inasistencias": 1
    },
    {
      "estudianteId": "EST-005",
      "codigo": "CON-101",
      "asignatura": "Contabilidad",
      "clasesProgramadas": 48,
      "inasistencias": 4
    },
    {
      "estudianteId": "EST-005",
      "codigo": "MIC-102",
      "asignatura": "Microeconomía",
      "clasesProgramadas": 48,
      "inasistencias": 5
    },
    {
      "estudianteId": "EST-005",
      "codigo": "EST-103",
      "asignatura": "Estadística",
      "clasesProgramadas": 64,
      "inasistencias": 6
    },
    {
      "estudianteId": "EST-005",
      "codigo": "COM-104",
      "asignatura": "Comunicación Oral y Escrita",
      "clasesProgramadas": 32,
      "inasistencias": 2
    },
    {
      "estudianteId": "EST-006",
      "codigo": "CON-101",
      "asignatura": "Contabilidad",
      "clasesProgramadas": 48,
      "inasistencias": 1
    },
    {
      "estudianteId": "EST-006",
      "codigo": "MIC-102",
      "asignatura": "Microeconomía",
      "clasesProgramadas": 48,
      "inasistencias": 0
    },
    {
      "estudianteId": "EST-006",
      "codigo": "EST-103",
      "asignatura": "Estadística",
      "clasesProgramadas": 64,
      "inasistencias": 2
    },
    {
      "estudianteId": "EST-006",
      "codigo": "COM-104",
      "asignatura": "Comunicación Oral y Escrita",
      "clasesProgramadas": 32,
      "inasistencias": 0
    }
  ]
};

crear(original);
