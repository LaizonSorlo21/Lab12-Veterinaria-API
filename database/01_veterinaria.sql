-- PostgreSQL. Ejecutar antes de generar las entidades con Scaffold.
-- Datos ficticios para el laboratorio. No borra tablas ni registros existentes.
BEGIN;
CREATE TABLE IF NOT EXISTS public.propietarios (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre varchar(100) NOT NULL CHECK (length(trim(nombre)) > 0),
    email varchar(150) NOT NULL CHECK (length(trim(email)) > 0),
    telefono varchar(20) NOT NULL,
    CONSTRAINT uq_propietarios_email UNIQUE (email)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_propietarios_email_normalizado
    ON public.propietarios (lower(trim(email)));
CREATE TABLE IF NOT EXISTS public.mascotas (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre varchar(80) NOT NULL CHECK (length(trim(nombre)) > 0),
    especie varchar(40) NOT NULL CHECK (length(trim(especie)) > 0),
    edad integer NOT NULL CHECK (edad BETWEEN 0 AND 50),
    peso numeric(6,2) NOT NULL CHECK (peso BETWEEN 0.01 AND 999.99),
    propietario_id integer NOT NULL,
    CONSTRAINT fk_mascotas_propietarios FOREIGN KEY (propietario_id)
        REFERENCES public.propietarios(id) ON DELETE RESTRICT,
    CONSTRAINT uq_mascotas_propietario_nombre UNIQUE (propietario_id, nombre)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_mascotas_nombre_normalizado
    ON public.mascotas (propietario_id, lower(trim(nombre)));
INSERT INTO public.propietarios(nombre,email,telefono) VALUES
('Ana Torres','ana.torres@example.com','900111222'),
('Luis Ramos','luis.ramos@example.com','900333444'),
('Carla Rojas','carla.rojas@example.com','900555666')
ON CONFLICT DO NOTHING;
INSERT INTO public.mascotas(nombre,especie,edad,peso,propietario_id)
SELECT m.nombre,m.especie,m.edad,m.peso,p.id
FROM (VALUES
('Luna','Perro',3,12.50,'ana.torres@example.com'),
('Milo','Gato',2,4.20,'ana.torres@example.com'),
('Rocky','Perro',5,20.00,'luis.ramos@example.com')
) AS m(nombre,especie,edad,peso,email)
JOIN public.propietarios p ON p.email=m.email
ON CONFLICT DO NOTHING;
COMMIT;
