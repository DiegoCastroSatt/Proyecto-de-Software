use optica_db;
SET NAMES utf8mb4;
-- ==========================================
-- CLIENTES
-- ==========================================

INSERT INTO clientes (rut, nombre, apellido, telefono, correo) VALUES
('12345678-7', 'Juan', 'Pérezino', '+56911111111', 'juan.perezi@test.cl'),
('98765432-5', 'María', 'González', '+56922222222', 'maria.gonzalez@test.cl'),
('17654321-0', 'Carlos', 'Muñoz', '+56933333333', 'carlos.munoz@test.cl'),
('15432109-8', 'Ana', 'Soto', '+56944444444', 'ana.soto@test.cl'),
('20123456-5', 'Pedro', 'Rojas', '+56955555555', 'pedro.rojas@test.cl');


-- ==========================================
-- RECETAS
-- ==========================================

INSERT INTO recetas (id_cliente, fecha, observaciones, imagen_path) VALUES
(1, '2026-09-01', 'Control visual anual.', NULL),
(2, '2026-09-03', 'Presenta dificultad para visión lejana.', 'recetas/receta_2.jpg'),
(3, '2026-09-05', 'Renovación de receta.', NULL),
(4, '2026-09-10', 'Primera evaluación visual.', 'recetas/receta_4.jpg'),
(5, '2026-09-15', 'Control por cambio de graduación.', NULL);


-- ==========================================
-- GRADUACIONES
-- ==========================================

INSERT INTO graduaciones
(id_receta, ojo, esfera, cilindro, eje, adicion) VALUES
(1, 'OD', -1.25, -0.50, 90, NULL),
(1, 'OI', -1.00, -0.25, 85, NULL),

(2, 'OD', -2.00, -0.75, 100, NULL),
(2, 'OI', -1.75, -0.50, 95, NULL),

(3, 'OD', 1.00, -0.25, 80, 1.50),
(3, 'OI', 1.25, -0.25, 85, 1.50),

(4, 'OD', -0.50, NULL, NULL, NULL),
(4, 'OI', -0.75, NULL, NULL, NULL),

(5, 'OD', 0.75, -0.50, 110, 2.00),
(5, 'OI', 1.00, -0.50, 105, 2.00);


-- ==========================================
-- PRODUCTOS
-- ==========================================

INSERT INTO productos
(codigo, nombre, marca, modelo, color, categoria, precio, stock, stock_minimo, estado, ruta_imagen)
VALUES
('RB001', 'Armazón Ray-Ban Classic', 'Ray-Ban', 'RB5228', 'Negro', 'Armazones',
89990, 10, 3, 'Disponible', 'https://cdn1.visiofactory.com/17473/ray-ban-rx5228-rb5228-2012-50-17-tortoise-small.jpg'),

('OA001', 'Armazón Oakley Metal', 'Oakley', 'OX3217', 'Negro', 'Armazones',
109990, 7, 2, 'Disponible', 'https://assets2.clearly.ca/cdn-record-files-pi/05862cba-376d-42f6-b670-a5ea00d876ef/d01719b6-29c6-4835-817c-ac9a005f075d/0OX3217__321703__STD__shad__fr.png?bgc=%23FFFFFF&impolicy=CLE_resize&wid=2400'),

('VG001', 'Lentes Vogue Classic', 'Vogue', 'VO5276', 'Café', 'Lentes ópticos',
79990, 5, 2, 'Disponible', 'https://www.fashioneyewear.com/cdn/shop/products/0vo5276__1916_030a.jpg?v=1643280019&width=1024'),

('PL001', 'Lentes de Sol Polaroid', 'Polaroid', 'PLD2053', 'Negro', 'Lentes de sol',
69990, 8, 2, 'Disponible', 'https://sometimes.cl/cdn/shop/files/716736369358_1_0b761f54-3667-4bc7-8455-7103938e0898.jpg?v=1733200281&width=1920'),

('ACC001', 'Estuche para lentes', '', '', 'Negro', 'Accesorios',
9990, 20, 5, 'Disponible', 'https://eokulary.com.pl/userdata/public/gfx/53179/czarne-etui-na-okulary-1000x900.png'),

('ACC002', 'Paño de limpieza', '', '', 'Negro', 'Accesorios',
2990, 30, 10, 'Disponible', 'https://img.danawa.com/prod_img/500000/274/907/img/12907274_1.jpg?_v=20201214141313&shrink=360%3A360'),

('ACC003', 'Líquido limpiador para lentes', 'ZEISS', 'Spray 30 ml', 'Transparente', 'Accesorios',
5990, 20, 5, 'Disponible', 'https://www.framepunk.com/cdn/shop/products/ZEISSLensCleander30ml_1024x1024.jpg?v=1635859601'),

('LC001', 'Lentes de contacto mensual', '', 'Mensual', 'Transparente', 'Lentes de contacto',
24990, 12, 4, 'Disponible', 'https://get2020.com.au/cdn/shop/files/Miru1Monthpack_lensesimage.jpg?crop=center&height=1200&v=1723100265&width=1200'),

('LC002', 'Lentes de uso diario', 'CooperVision', 'MyDay 30 unidades', 'Transparente', 'Lentes de contacto',
32990, 18, 5, 'Disponible', 'https://assets2.visiondirect.es/cdn-record-files/contactlenses/8c5a2f12-7a57-4c56-9a7a-afb600a1e656/CVCVMDA030__shad__fr5.png?bgc=%23FFFFFF&impolicy=VD_resize&wid=600'),

('ACC004', 'Cordón ajustable para lentes', '', '', 'Negro', 'Accesorios',
4990, 25, 8, 'Disponible', 'https://images.tcdn.com.br/img/img_prod/1165337/cordao_de_oculos_ajustavel_antiderrapante_para_oculos_casuais_e_esportivos_preto_26828_1_6e53c362498ef89271e0b79be804f540.jpg'),

('ACC005', 'Toallitas húmedas para lentes', '', '', 'Blanco', 'Accesorios',
3990, 40, 10, 'Disponible', 'https://www.hema.nl/dw/image/v2/BBRK_PRD/on/demandware.static/-/Sites-HEMA-master-catalog/default/dw6fffcb47/product/11510070_01_001.jpg?bgcolor=FFFFFF&sfrm=png&sw=600');


-- ==========================================
-- PEDIDOS
-- ==========================================

INSERT INTO pedidos
(id_cliente, id_receta, fecha, estado, total, anotaciones) VALUES
(1, 1, '2026-09-02 10:30:00', 'Entregado', 99980, 'Armazón Ray-Ban con cristales antirreflejo.'),
(2, 2, '2026-09-05 12:15:00', 'En proceso', 109990, 'Solicita aviso telefónico cuando el pedido esté listo.'),
(3, 3, '2026-09-08 16:20:00', 'Listo', 79990, 'Pendiente de retiro en sucursal.'),
(4, 4, '2026-09-12 11:00:00', 'Pendiente', 69990, 'Primera evaluación; confirmar modelo antes de preparar.'),
(5, 5, '2026-09-16 17:30:00', 'En proceso', 89990, 'Cambio de graduación y ajuste de armazón incluido.');


-- ==========================================
-- VENTAS
-- Se usan distintos meses para probar tus gráficos
-- ==========================================

INSERT INTO ventas (fecha, total) VALUES
('2025-10-15 10:30:00', 89990),
('2025-11-20 15:10:00', 109990),
('2025-12-12 12:00:00', 129970),
('2026-01-18 17:30:00', 79990),
('2026-02-10 11:15:00', 69990),
('2026-03-22 16:45:00', 99980),
('2026-04-08 13:20:00', 109990),
('2026-05-17 10:10:00', 89990),
('2026-06-25 18:00:00', 119980),
('2026-07-14 14:30:00', 79990),
('2026-08-21 12:40:00', 139980),
('2026-09-05 10:00:00', 89990),
('2026-09-12 16:30:00', 119980),
('2026-09-18 11:20:00', 99980),
('2025-10-28 12:00:00', 99980),
('2025-11-08 16:15:00', 114980),
('2025-12-21 11:30:00', 40970),
('2026-01-27 14:00:00', 84980),
('2026-02-19 10:45:00', 74980),
('2026-03-11 17:10:00', 95980),
('2026-04-23 12:30:00', 114980),
('2026-05-29 15:20:00', 84980),
('2026-06-16 11:00:00', 74980),
('2026-07-30 16:40:00', 95980),
('2026-08-12 13:15:00', 40970),
('2026-09-20 10:30:00', 114980);


-- ==========================================
-- DETALLE VENTA
-- ==========================================

INSERT INTO detalle_venta
(id_venta, id_producto, cantidad, precio_unitario, subtotal) VALUES
(1, 1, 1, 89990, 89990),
(2, 2, 1, 109990, 109990),

(3, 3, 1, 79990, 79990),
(3, 5, 5, 9990, 49950),

(4, 3, 1, 79990, 79990),
(5, 4, 1, 69990, 69990),

(6, 1, 1, 89990, 89990),
(6, 5, 1, 9990, 9990),

(7, 2, 1, 109990, 109990),
(8, 1, 1, 89990, 89990),

(9, 2, 1, 109990, 109990),
(9, 5, 1, 9990, 9990),

(10, 3, 1, 79990, 79990),

(11, 4, 2, 69990, 139980),

(12, 1, 1, 89990, 89990),

(13, 2, 1, 109990, 109990),
(13, 5, 1, 9990, 9990),

(14, 1, 1, 89990, 89990),
(14, 5, 1, 9990, 9990),

(15, 1, 1, 89990, 89990),
(15, 5, 1, 9990, 9990),

(16, 2, 1, 109990, 109990),
(16, 10, 1, 4990, 4990),

(17, 9, 1, 32990, 32990),
(17, 11, 2, 3990, 7980),

(18, 3, 1, 79990, 79990),
(18, 10, 1, 4990, 4990),

(19, 4, 1, 69990, 69990),
(19, 10, 1, 4990, 4990),

(20, 1, 1, 89990, 89990),
(20, 7, 1, 5990, 5990),

(21, 2, 1, 109990, 109990),
(21, 10, 1, 4990, 4990),

(22, 3, 1, 79990, 79990),
(22, 10, 1, 4990, 4990),

(23, 4, 1, 69990, 69990),
(23, 10, 1, 4990, 4990),

(24, 1, 1, 89990, 89990),
(24, 7, 1, 5990, 5990),

(25, 9, 1, 32990, 32990),
(25, 11, 2, 3990, 7980),

(26, 2, 1, 109990, 109990),
(26, 10, 1, 4990, 4990);


-- ==========================================
-- ADMINISTRADORES
-- Contraseñas ficticias para datos de prueba
-- En producción deben guardarse como hash
-- ==========================================

INSERT INTO administradores
(nombre, correo, contrasena, estado) VALUES
('Administrador Principal', 'admin@optica.cl', 'HASH_PRUEBA_ADMIN_1', 'Activo'),
('Pablo Martinez', 'pablo@optica.cl', 'HASH_PRUEBA_ADMIN_2', 'Activo');


-- ==========================================
-- HORARIOS DE ATENCION
-- ==========================================

INSERT INTO horarios_atencion
(id_administrador, fecha, hora_inicio, hora_fin, estado) VALUES
(1, '2026-09-22', '09:00:00', '09:30:00', 'Habilitada'),
(1, '2026-09-22', '09:30:00', '10:00:00', 'Habilitada'),
(1, '2026-09-22', '10:00:00', '10:30:00', 'Habilitada'),
(1, '2026-09-23', '09:00:00', '09:30:00', 'Habilitada'),
(1, '2026-09-23', '09:30:00', '10:00:00', 'Habilitada'),
(2, '2026-09-24', '15:00:00', '15:30:00', 'Habilitada'),
(2, '2026-09-24', '15:30:00', '16:00:00', 'Inhabilitada');


-- ==========================================
-- RESERVAS
-- ==========================================

INSERT INTO reservas
(id_cliente, id_horario, motivo, estado) VALUES
(1, 1, 'Control de visión', 'Confirmada'),
(2, 2, 'Cambio de lentes', 'Pendiente'),
(3, 4, 'Control de graduación', 'Confirmada'),
(4, 6, 'Primera consulta', 'Pendiente');
