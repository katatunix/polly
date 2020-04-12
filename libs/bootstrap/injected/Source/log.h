#pragma once

#include <stdio.h>

#define LOGI(...)		{ char buf[1024]; sprintf(buf, __VA_ARGS__); printf("Polly: %s\n", buf); }
#define LOGW(...)		{ char buf[1024]; sprintf(buf, __VA_ARGS__); printf("Polly: [WARN] %s\n", buf); }
#define LOGD(...)		{ char buf[1024]; sprintf(buf, __VA_ARGS__); printf("Polly: [DBG] %s\n", buf); }
#define LOGE(...)		{ char buf[1024]; sprintf(buf, __VA_ARGS__); printf("Polly: [ERR] %s\n", buf); }
