USE optica_db;
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

INSERT IGNORE INTO catalogos (tipo, nombre) VALUES
('Marca', 'Ray-Ban'),
('Marca', 'Oakley'),
('Marca', 'Vogue'),
('Marca', 'Polaroid'),
('Color', 'Negro'),
('Color', 'Café'),
('Color', 'Dorado'),
('Color', 'Plateado'),
('Color', 'Transparente'),
('Categoria', 'Lentes ópticos'),
('Categoria', 'Lentes de sol'),
('Categoria', 'Armazones'),
('Categoria', 'Lentes de contacto'),
('Categoria', 'Accesorios');


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
(rut, id_receta, fecha, estado, total, anotaciones) VALUES
('12345678-7', 1, '2026-09-02 10:30:00', 'Entregado', 99980, 'Armazón Ray-Ban con cristales antirreflejo.'),
('98765432-5', 2, '2026-09-05 12:15:00', 'En proceso', 109990, 'Solicita aviso telefónico cuando el pedido esté listo.'),
('17654321-0', 3, '2026-09-08 16:20:00', 'Listo', 79990, 'Pendiente de retiro en sucursal.'),
('15432109-8', 4, '2026-09-12 11:00:00', 'Pendiente', 69990, 'Primera evaluación; confirmar modelo antes de preparar.'),
('20123456-5', 5, '2026-09-16 17:30:00', 'En proceso', 89990, 'Cambio de graduación y ajuste de armazón incluido.');


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



INSERT INTO detalle_venta
(id_venta, id_producto, nombre_producto, cantidad, precio_unitario, subtotal) VALUES
(1, 1, 'Armazón Ray-Ban Classic', 1, 89990, 89990),
(2, 2, 'Armazón Oakley Metal', 1, 109990, 109990),

(3, 3, 'Lentes Vogue Classic', 1, 79990, 79990),
(3, 5, 'Estuche para lentes', 5, 9990, 49950),

(4, 3, 'Lentes Vogue Classic', 1, 79990, 79990),
(5, 4, 'Lentes de Sol Polaroid', 1, 69990, 69990),

(6, 1, 'Armazón Ray-Ban Classic', 1, 89990, 89990),
(6, 5, 'Estuche para lentes', 1, 9990, 9990),

(7, 2, 'Armazón Oakley Metal', 1, 109990, 109990),
(8, 1, 'Armazón Ray-Ban Classic', 1, 89990, 89990),

(9, 2, 'Armazón Oakley Metal', 1, 109990, 109990),
(9, 5, 'Estuche para lentes', 1, 9990, 9990),

(10, 3, 'Lentes Vogue Classic', 1, 79990, 79990),

(11, 4, 'Lentes de Sol Polaroid', 2, 69990, 139980),

(12, 1, 'Armazón Ray-Ban Classic', 1, 89990, 89990),

(13, 2, 'Armazón Oakley Metal', 1, 109990, 109990),
(13, 5, 'Estuche para lentes', 1, 9990, 9990),

(14, 1, 'Armazón Ray-Ban Classic', 1, 89990, 89990),
(14, 5, 'Estuche para lentes', 1, 9990, 9990),

(15, 1, 'Armazón Ray-Ban Classic', 1, 89990, 89990),
(15, 5, 'Estuche para lentes', 1, 9990, 9990),

(16, 2, 'Armazón Oakley Metal', 1, 109990, 109990),
(16, 10, 'Cordón ajustable para lentes', 1, 4990, 4990),

(17, 9, 'Lentes de uso diario', 1, 32990, 32990),
(17, 11, 'Toallitas húmedas para lentes', 2, 3990, 7980),

(18, 3, 'Lentes Vogue Classic', 1, 79990, 79990),
(18, 10, 'Cordón ajustable para lentes', 1, 4990, 4990),

(19, 4, 'Lentes de Sol Polaroid', 1, 69990, 69990),
(19, 10, 'Cordón ajustable para lentes', 1, 4990, 4990),

(20, 1, 'Armazón Ray-Ban Classic', 1, 89990, 89990),
(20, 7, 'Líquido limpiador para lentes', 1, 5990, 5990),

(21, 2, 'Armazón Oakley Metal', 1, 109990, 109990),
(21, 10, 'Cordón ajustable para lentes', 1, 4990, 4990),

(22, 3, 'Lentes Vogue Classic', 1, 79990, 79990),
(22, 10, 'Cordón ajustable para lentes', 1, 4990, 4990),

(23, 4, 'Lentes de Sol Polaroid', 1, 69990, 69990),
(23, 10, 'Cordón ajustable para lentes', 1, 4990, 4990),

(24, 1, 'Armazón Ray-Ban Classic', 1, 89990, 89990),
(24, 7, 'Líquido limpiador para lentes', 1, 5990, 5990),

(25, 9, 'Lentes de uso diario', 1, 32990, 32990),
(25, 11, 'Toallitas húmedas para lentes', 2, 3990, 7980),

(26, 2, 'Armazón Oakley Metal', 1, 109990, 109990),
(26, 10, 'Cordón ajustable para lentes', 1, 4990, 4990);



INSERT INTO administradores
(nombre, correo, contrasena, estado) VALUES
('ale', 'alejandro.gamergood@gmail.com', '1234', 'Activo');

-- TIPOS DE MARCOS
INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'rectangular',
  'Rectangular',
  'Rectangular',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M24 42 Q22 37 30 37 H91 Q100 37 98 45 L94 73 Q93 79 85 79 H38 Q30 79 28 72 Z"/><path d="M24 42 Q22 37 30 37 H91 Q100 37 98 45 L94 73 Q93 79 85 79 H38 Q30 79 28 72 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'corazon,ovalado,redondo'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'redondo',
  'Redondo',
  'Redondo',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M98 60 A36 36 0 1 1 26 60 A36 36 0 1 1 98 60 Z"/><path d="M98 60 A36 36 0 1 1 26 60 A36 36 0 1 1 98 60 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'alargado,corazon,cuadrado,diamante,ovalado'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'ovalado',
  'Ovalado',
  'Ovalado',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M100 60 A39 28 0 1 1 22 60 A39 28 0 1 1 100 60 Z"/><path d="M100 60 A39 28 0 1 1 22 60 A39 28 0 1 1 100 60 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'corazon,cuadrado,diamante,ovalado'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'cuadrado',
  'Cuadrado',
  'Cuadrado',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M30 29 Q25 29 25 36 L28 83 Q28 89 35 89 H87 Q95 89 95 81 L98 36 Q98 29 90 29 Z"/><path d="M30 29 Q25 29 25 36 L28 83 Q28 89 35 89 H87 Q95 89 95 81 L98 36 Q98 29 90 29 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'alargado,ovalado,redondo'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'cat-eye',
  'Cat eye',
  'Cat eye',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M16 29 Q54 43 91 39 Q102 39 99 53 L92 75 Q85 88 57 85 Q32 82 25 62 Z"/><path d="M16 29 Q54 43 91 39 Q102 39 99 53 L92 75 Q85 88 57 85 Q32 82 25 62 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'diamante,ovalado,redondo'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'aviador',
  'Aviador',
  'Aviador',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M23 39 Q23 28 42 29 L89 36 Q102 39 98 54 Q88 91 66 94 Q39 96 27 69 Q22 57 23 39 Z"/><path d="M23 39 Q23 28 42 29 L89 36 Q102 39 98 54 Q88 91 66 94 Q39 96 27 69 Q22 57 23 39 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/><path d="M100 36 Q120 29 140 36"/></g></svg>',
  'alargado,corazon,cuadrado,diamante,ovalado'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'wayfarer',
  'Wayfarer',
  'Wayfarer',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M19 34 Q60 27 101 35 L92 77 Q89 85 78 86 H43 Q33 85 29 76 Z"/><path d="M19 34 Q60 27 101 35 L92 77 Q89 85 78 86 H43 Q33 85 29 76 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'alargado,redondo'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'geometrico',
  'Geométrico',
  'Geométrico',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M23 44 L43 25 H78 L100 44 L94 76 L76 90 H43 L26 74 Z"/><path d="M23 44 L43 25 H78 L100 44 L94 76 L76 90 H43 L26 74 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'ovalado,redondo'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'browline',
  'Browline',
  'Browline',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M23 42 Q58 28 97 41 L94 65 Q89 87 61 88 Q34 86 27 67 Z"/><path d="M23 42 Q58 28 97 41 L94 65 Q89 87 61 88 Q34 86 27 67 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g><path d="M23 42 Q58 28 97 41 M143 41 Q182 28 217 42" fill="none" stroke="#222222" stroke-width="7" stroke-linecap="round"/></svg>',
  'alargado,diamante,ovalado,redondo'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'semimontura',
  'Semimontura',
  'Semimontura',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="1.3" stroke-linejoin="round"><path d="M24 42 Q60 27 98 42 L94 70 Q89 87 62 87 Q35 86 28 69 Z"/><path d="M24 42 Q60 27 98 42 L94 70 Q89 87 62 87 Q35 86 28 69 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g><path d="M23 42 Q58 28 97 41 M143 41 Q182 28 217 42" fill="none" stroke="#222222" stroke-width="7" stroke-linecap="round"/></svg>',
  'corazon,cuadrado,diamante,ovalado'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'sin-montura-cuadrado',
  'Sin montura cuadrado',
  'Sin montura',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M101 44 Q120 34 139 44"/><path d="M26 43 L9 39 M214 43 L231 39"/></g><g fill="#222222"><circle cx="101" cy="44" r="2.5"/><circle cx="139" cy="44" r="2.5"/><circle cx="26" cy="43" r="2.5"/><circle cx="214" cy="43" r="2.5"/></g></svg>',
  'ovalado,redondo,corazon,diamante'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'sin-montura',
  'Sin montura',
  'Sin montura',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="1.3" stroke-linejoin="round"><path d="M100 60 A39 28 0 1 1 22 60 A39 28 0 1 1 100 60 Z"/><path d="M100 60 A39 28 0 1 1 22 60 A39 28 0 1 1 100 60 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'corazon,cuadrado,diamante,ovalado'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'transparente',
  'Transparente',
  'Transparente',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="6" stroke-linejoin="round"><path d="M25 35 Q61 26 95 35 Q102 38 99 49 L94 77 Q91 86 80 86 H43 Q31 86 28 75 L23 48 Q21 39 25 35 Z"/><path d="M25 35 Q61 26 95 35 Q102 38 99 49 L94 77 Q91 86 80 86 H43 Q31 86 28 75 L23 48 Q21 39 25 35 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'corazon'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'rectangular-alto',
  'Rectangular alto',
  'Rectangular alto',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M25 32 Q23 27 32 27 H91 Q100 27 98 36 L94 82 Q93 90 84 90 H39 Q30 90 28 81 Z"/><path d="M25 32 Q23 27 32 27 H91 Q100 27 98 36 L94 82 Q93 90 84 90 H39 Q30 90 28 81 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'alargado'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'aviador-angular',
  'Aviador angular',
  'Aviador angular',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M24 32 L88 37 L100 48 L84 86 L61 95 L34 83 L23 58 Z"/><path d="M24 32 L88 37 L100 48 L84 86 L61 95 L34 83 L23 58 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/><path d="M100 36 Q120 29 140 36"/></g></svg>',
  'redondo'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'cat-eye-suave',
  'Cat eye suave',
  'Cat eye suave',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M20 35 Q55 42 88 40 Q101 41 98 57 Q93 83 65 85 Q37 86 28 65 Z"/><path d="M20 35 Q55 42 88 40 Q101 41 98 57 Q93 83 65 85 Q37 86 28 65 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'cuadrado'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'mariposa',
  'Mariposa',
  'Mariposa',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M14 27 Q42 30 94 44 Q103 47 98 63 Q91 89 64 91 Q33 92 24 67 Z"/><path d="M14 27 Q42 30 94 44 Q103 47 98 63 Q91 89 64 91 Q33 92 24 67 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'cuadrado'
);

INSERT IGNORE INTO tipos_marco
(codigo, nombre, categoria, svg, rostros)
VALUES (
  'oversize',
  'Oversize',
  'Oversize',
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g fill="none" stroke="#222222" stroke-width="4" stroke-linejoin="round"><path d="M19 28 Q54 17 95 30 Q106 35 100 63 L91 89 Q88 97 70 98 H43 Q25 96 22 81 L17 48 Q15 34 19 28 Z"/><path d="M19 28 Q54 17 95 30 Q106 35 100 63 L91 89 Q88 97 70 98 H43 Q25 96 22 81 L17 48 Q15 34 19 28 Z" transform="translate(240 0) scale(-1 1)"/></g><g stroke="#222222" stroke-width="3" stroke-linecap="round"><path d="M100 49 Q120 38 140 49 M22 44 L9 40 M218 44 L231 40"/></g></svg>',
  'alargado'
);


-- TIPOS DE ROSTRO

ALTER TABLE tipos_marco
MODIFY COLUMN rostros TEXT NOT NULL;

INSERT IGNORE INTO tipos_rostro
(codigo, nombre, rasgos, consejo, ruta_imagen)
VALUES (
  'ovalado',
  'Ovalado',
  'Un poco más largo que ancho, con mandíbula suavemente redondeada.',
  'Prueba distintas formas y mantén el ancho del marco cerca del ancho de tus pómulos.',
  '/assets/images/recomendador/rostro-ovalado.png'
);

INSERT IGNORE INTO tipos_rostro
(codigo, nombre, rasgos, consejo, ruta_imagen)
VALUES (
  'redondo',
  'Redondo',
  'Mejillas llenas, contornos suaves y ancho similar al largo.',
  'Si buscas contraste, elige líneas definidas y un marco que no quede demasiado pequeño.',
  '/assets/images/recomendador/rostro-redondo.png'
);

INSERT IGNORE INTO tipos_rostro
(codigo, nombre, rasgos, consejo, ruta_imagen)
VALUES (
  'cuadrado',
  'Cuadrado',
  'Frente amplia y mandíbula definida, con anchos parecidos.',
  'Las curvas y los perfiles finos pueden dar un contraste suave a la mandíbula.',
  '/assets/images/recomendador/rostro-cuadrado.png'
);

INSERT IGNORE INTO tipos_rostro
(codigo, nombre, rasgos, consejo, ruta_imagen)
VALUES (
  'alargado',
  'Alargado',
  'El largo predomina sobre el ancho, con mejillas más estrechas.',
  'Prueba lentes con más altura y evita que el marco quede demasiado estrecho.',
  '/assets/images/recomendador/rostro-alargado.png'
);

INSERT IGNORE INTO tipos_rostro
(codigo, nombre, rasgos, consejo, ruta_imagen)
VALUES (
  'corazon',
  'Corazón',
  'Frente más amplia y rostro que se estrecha hacia el mentón.',
  'Los perfiles ligeros y las curvas suaves ayudan a que el marco no domine la frente.',
  '/assets/images/recomendador/rostro-corazon.png'
);

INSERT IGNORE INTO tipos_rostro
(codigo, nombre, rasgos, consejo, ruta_imagen)
VALUES (
  'diamante',
  'Diamante',
  'Pómulos destacados, con frente y mentón más estrechos.',
  'Busca un ajuste cómodo sobre los pómulos y prueba detalles en la parte superior.',
  '/assets/images/recomendador/rostro-diamante.png'
);

-- Catálogo inicial de formas de lentes; el editor lo obtiene desde la API.
INSERT IGNORE INTO formas_lente (codigo, nombre, nombre_normalizado, contorno, geometria, mantener_proporcion) VALUES
('rectangular', 'Rectangular', 'rectangular', '', 'rectangulo', FALSE),
('ovalada', 'Ovalada', 'ovalada', '', 'elipse', FALSE),
('redonda', 'Redonda', 'redonda', '', 'elipse', TRUE),
('cuadrada', 'Cuadrada', 'cuadrada', '', 'rectangulo', TRUE),
('aviador', 'Aviador', 'aviador', 'M.04 .12 Q.2 .02 .42 .08 L.88 .17 Q1 .2 1 .28 Q1 .55 .75 .86 Q.58 1 .38 .96 Q.1 .91 .03 .6 Q0 .35 .04 .12Z', 'contorno', FALSE),
('cat-eye', 'Cat eye', 'cat eye', 'M.02 0 Q.4 .2 .85 .15 Q1 .14 1 .28 L.9 .68 Q.8 .95 .45 .9 Q.1 .84 .03 .48 L0 .28Z', 'contorno', FALSE),
('wayfarer', 'Wayfarer', 'wayfarer', 'M.05 .05 Q.4 0 .88 .08 Q1 .1 1 .28 L.85 .87 Q.82 .98 .64 1 H.28 Q.1 .98 .08 .8 L0 .28Q0 .1 .05 .05Z', 'contorno', FALSE),
('hexagonal', 'Hexagonal', 'hexagonal', 'M.18 0 H.82 L1 .28 L.82 1 H.18 L0 .28Z', 'contorno', FALSE),
('octagonal', 'Octagonal', 'octagonal', 'M.2 0 H.8 L1 .28 V.72 L.8 1 H.2 L0 .72 V.28Z', 'contorno', FALSE),
('mariposa', 'Mariposa', 'mariposa', 'M.04 0 Q.45 .03 .9 .2 Q1 .22 1 .28 Q1 .6 .8 .86 Q.65 1 .35 .96 Q.08 .9 .02 .55 L0 .28Z', 'contorno', FALSE);
