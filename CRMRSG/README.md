# CRM Management System - Documentación

## Descripción
Sistema de Gestión de Relaciones con Clientes (CRM) desarrollado con ASP.NET MVC y .NET Framework 4.8.1

## Estructura del Proyecto

### Controllers Creados

#### 1. **DashboardController**
- Archivo: `CRMRSG/Controllers/DashboardController.cs`
- Métodos:
  - `Index()` - Panel principal del CRM con KPIs y estadísticas

#### 2. **ClientesController**
- Archivo: `CRMRSG/Controllers/ClientesController.cs`
- Métodos:
  - `Index()` - Lista de clientes
  - `Crear()` - Crear nuevo cliente
  - `Editar(id)` - Editar cliente existente
  - `Detalle(id)` - Ver detalle del cliente

#### 3. **OportunidadesController**
- Archivo: `CRMRSG/Controllers/OportunidadesController.cs`
- Métodos:
  - `Index()` - Lista de oportunidades de venta
  - `Crear()` - Crear nueva oportunidad
  - `Editar(id)` - Editar oportunidad
  - `Detalle(id)` - Ver detalle de oportunidad

#### 4. **TareasController**
- Archivo: `CRMRSG/Controllers/TareasController.cs`
- Métodos:
  - `Index()` - Lista de tareas
  - `Crear()` - Crear nueva tarea
  - `Editar(id)` - Editar tarea

#### 5. **ActividadesController**
- Archivo: `CRMRSG/Controllers/ActividadesController.cs`
- Métodos:
  - `Index()` - Lista de actividades
  - `Crear()` - Crear nueva actividad

### Vistas CSHTML Creadas

#### Dashboard
- `CRMRSG/Views/Dashboard/Index.cshtml`
- Incluye KPIs con datos ficticios:
  - Clientes Totales: 145
  - Oportunidades Activas: 32
  - Tareas Pendientes: 18
  - Ingresos Potenciales: $425K
- Gráficos de oportunidades por etapa y clientes por región
- Últimas actividades y tareas próximas

#### Clientes
- `CRMRSG/Views/Clientes/Index.cshtml` - Listado de clientes con tabla
- `CRMRSG/Views/Clientes/Crear.cshtml` - Formulario para crear nuevo cliente
- Campos de formulario:
  - Nombre de Empresa
  - NIF/CIF
  - Contacto Principal
  - Cargo
  - Email
  - Teléfono
  - Industria
  - Tamaño de Empresa
  - Dirección completa
  - Notas

#### Oportunidades
- `CRMRSG/Views/Oportunidades/Index.cshtml` - Listado de oportunidades
- `CRMRSG/Views/Oportunidades/Crear.cshtml` - Formulario para nueva oportunidad
- Etapas del pipeline:
  - Identificación
  - Calificación
  - Propuesta
  - Negociación
  - Cierre
- Campos incluidos:
  - Nombre de oportunidad
  - Cliente asociado
  - Valor y probabilidad de cierre
  - Fecha de cierre estimada
  - Responsable

#### Tareas
- `CRMRSG/Views/Tareas/Index.cshtml` - Listado de tareas
- `CRMRSG/Views/Tareas/Crear.cshtml` - Formulario para crear tarea
- Estados:
  - Pendiente
  - En Progreso
  - Completada
- Prioridades:
  - Urgente
  - Normal
  - Baja
- Funcionalidades:
  - Vinculación con clientes y oportunidades
  - Seguimiento de progreso
  - Fechas de vencimiento

## Datos Ficticios

Se incluyen datos quemados en la capa de presentación para demostración:

### Clientes
1. Acme Corporation - Tecnología
2. Tech Solutions Inc - Software
3. Global Enterprises - Consultoría
4. Innovation Labs - Investigación
5. Digital Marketing Co - Marketing

### Oportunidades
1. Implementación Sistema ERP - $45,000 - 60% probabilidad
2. Desarrollo App Móvil - $28,000 - 75% probabilidad
3. Consultoría Digital - $15,000 - 30% probabilidad
4. Campaña Marketing Digital - $22,000 - 100% (Ganada)
5. Análisis de Datos - $18,000 - 10% (Perdida)

### Tareas
- Distribuidas con diferentes prioridades y estados
- Asociadas a clientes específicos
- Con fechas y horas de vencimiento

## Template Base
- Template: Alfa Admin Dashboard
- Tecnología: Bootstrap 5
- Iconos: Feather Icons, Font Awesome
- Gráficos: C3.js, ECharts, ApexCharts
- Skin: Tema Primario
- Responsive: Sí (Mobile First)

## Navegación

El menú lateral incluye:
- **Dashboard** - Panel principal
- **Clientes** - Gestión de clientes
- **Oportunidades** - Pipeline de ventas
- **Tareas** - Gestor de tareas
- **Actividades** - Registro de actividades
- **Soporte** - Sistema de tickets y chat

## Rutas de Navegación

```
/Dashboard/Index - Panel Principal
/Clientes - Listar Clientes
/Clientes/Crear - Crear Cliente
/Clientes/Editar/{id} - Editar Cliente
/Clientes/Detalle/{id} - Ver Detalle
/Oportunidades - Listar Oportunidades
/Oportunidades/Crear - Crear Oportunidad
/Oportunidades/Editar/{id} - Editar Oportunidad
/Tareas - Listar Tareas
/Tareas/Crear - Crear Tarea
/Tareas/Editar/{id} - Editar Tarea
/Actividades - Listar Actividades
/Actividades/Crear - Crear Actividad
```

## Próximos Pasos

Para completar la funcionalidad, se recomienda:
1. Crear modelos de datos para Clientes, Oportunidades, Tareas, etc.
2. Implementar Entity Framework para acceso a base de datos
3. Agregar validaciones en los controllers
4. Implementar autenticación y autorización
5. Conectar gráficos con datos reales
6. Agregar paginación en listados
7. Implementar búsqueda y filtros avanzados
8. Agregar reportes y exportación a Excel/PDF

## Notas Técnicas

- Framework: .NET Framework 4.8.1
- Patrón: MVC (Model-View-Controller)
- Actualmente: Datos ficticios en la presentación
- Próximas fases: Integración con base de datos
