// Types used by Orbbec cameras
// The definitions were originally part of the "cmd" API and are now used in the Orbbec OpenNI extension API.

#pragma once

typedef struct OBCameraParams
{
	float l_intr_p[4];//[fx,fy,cx,cy]
	float r_intr_p[4];//[fx,fy,cx,cy]
	float r2l_r[9];//[r00,r01,r02;r10,r11,r12;r20,r21,r22]
	float r2l_t[3];//[t1,t2,t3]
	float l_k[5];//[k1,k2,k3,p1,p2]
	float r_k[5];//[k1,k2,k3,p1,p2]
	int is_m;
} OBCameraParams;

#define IsMirroredTrue		1
#define IsMirroredFalse		2
