shader_type spatial;

#include "ShaderExpand/HexMetrics.gdshaderinc"

void fragment() {
	ALBEDO = COLOR.rgb;
}