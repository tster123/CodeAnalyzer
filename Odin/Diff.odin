package main

import "core:os"
import "core:fmt"
import "core:image"
import "core:image/bmp"
import "core:math/rand"
import "core:strconv"
import "core:strings"

PatienceSection :: struct {
	a_low : uint,
	a_high : uint,
    b_low : uint,
    b_high : uint,
}


SingleOccurence :: struct {
    location : int,
    value : u64,
    multiple_seen : bool,
}

main :: proc() {
	width, height, radius, fineness : int;
	ok : bool;
	width, ok = strconv.parse_int(os.args[1])
	if !ok {
		fmt.panicf("Invalid width [%v]", os.args[1]);
	}
	height, ok = strconv.parse_int(os.args[2])
	if !ok {
		fmt.panicf("Invalid height [%v]", os.args[2]);
	}
	radius, ok = strconv.parse_int(os.args[3])
	if !ok {
		fmt.panicf("Invalid radius [%v]", os.args[3]);
	}
	fineness, ok = strconv.parse_int(os.args[4])
	if !ok {
		fmt.panicf("Invalid fineness [%v]", os.args[4]);
	}
	fmt.printfln("Width=%v, Height=%v, Radius=%v, Fineness=%v", width, height, radius, fineness);
	if (width % fineness != 0 || height % fineness != 0) {
		panic("Height and Width must be evenly divisible by fineness")
	}
	points : ^[]Point = generate_blue_noise(width, height, radius, fineness);
	write_image(points, width, height);
}

get_singles :: proc(list: ^[]uint64) -> ^[]SingleOccurence {
	numMultiples : int;
    occur : []SingleOccurence = make_slice([]SingleOccurence, min(10, len(list)));

} 

write_image :: proc(points: ^[] Point, width: int, height: int) {
	pixels := make_slice([]bmp.RGB_Pixel, height * width);
	for p in points {
		minX := max(0, p.x - 2);
		maxX := min(width, p.x + 2);
		minY := max(0, p.y - 2);
		maxY := min(height, p.y + 2);
		for x in minX..=maxX {
			for y in minY..=maxY {
				pixels[y * width + x] = {255, 0, 0}
			}
		}
	}
	img:image.Image
	ok:bool
	img, ok = image.pixels_to_image(pixels, width, height)
	if !ok {
		panic("Error making image");
	}
	bmp.save_to_file("C:\\temp\\odin.bmp", &img);
}