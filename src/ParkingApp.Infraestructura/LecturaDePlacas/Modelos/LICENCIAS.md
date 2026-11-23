# Modelos del lector de placas

Los dos modelos se usan sin cambios, tal como los publica su autor, y corren en el mismo equipo: la imagen no sale de él.

| Archivo | Qué hace | Origen | Licencia | SHA-256 |
|---|---|---|---|---|
| `yolo-v9-t-384-license-plates-end2end.onnx` | Ubica la placa en la foto | [open-image-models](https://github.com/ankandrew/open-image-models), publicación `assets` | El código es MIT. El modelo se entrenó con una variante de [YOLOv9](https://github.com/WongKinYiu/yolov9), que es GPL-3.0, y su autor no sabe si esa licencia alcanza a los pesos ([consulta](https://github.com/ankandrew/open-image-models/issues/22)) | `888397b96d761c89db40bc9c305838e8652660f5e282c2cadebbe8d2951a77a8` |
| `cct_xs_v2_global.onnx` | Lee el texto de la placa | [fast-plate-ocr](https://github.com/ankandrew/fast-plate-ocr), publicación `arg-plates` | MIT, también los pesos ([consulta](https://github.com/ankandrew/fast-plate-ocr/issues/99)) | `8031afb5fdc6b4d80462c9d542f1284ebd2cfddf5dbacd62609848d7e2855f44` |

Los dos los reúne [fast-alpr](https://github.com/ankandrew/fast-alpr) (MIT), que es el sistema que cita el enunciado entre las opciones locales de lectura de placas.
