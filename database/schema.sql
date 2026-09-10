create table usuarios
(
    id_usuario integer auto_increment primary key,
    email varchar(100) not null,
    nombre varchar(100) not null,
    apellido_paterno varchar(100) not null,
    apellido_materno varchar(100),
    fecha_nacimiento datetime not null,
    telefono bigint,
    genero char(1)
);
create table fotos_usuarios
(
    id_foto integer auto_increment primary key,
    foto longblob,
    id_usuario integer not null
);
alter table fotos_usuarios add foreign key (id_usuario) references usuarios(id_usuario);
create unique index usuarios_1 on usuarios(email);

-- 3.1 Crear la tabla stock
CREATE TABLE stock (
    id_articulo INTEGER AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    descripcion TEXT,
    precio DECIMAL(10, 2) NOT NULL,
    cantidad INTEGER NOT NULL
);

-- 3.2 Crear la tabla fotos_articulos
CREATE TABLE fotos_articulos (
    id_foto INTEGER AUTO_INCREMENT PRIMARY KEY,
    foto LONGBLOB NOT NULL,
    id_articulo INTEGER NOT NULL,
    FOREIGN KEY (id_articulo) REFERENCES stock(id_articulo)
);

-- 3.3 Crear la tabla carrito_compra
CREATE TABLE carrito_compra (
    id_usuario INTEGER NOT NULL,
    id_articulo INTEGER NOT NULL,
    cantidad INTEGER NOT NULL,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario),
    FOREIGN KEY (id_articulo) REFERENCES stock(id_articulo),
    UNIQUE KEY idx_carrito_compra (id_usuario, id_articulo)
);

-- 3.5 Modificar la tabla usuarios para agregar los campos password y token
ALTER TABLE usuarios
ADD COLUMN password VARCHAR(20) NOT NULL,
ADD COLUMN token VARCHAR(20);
