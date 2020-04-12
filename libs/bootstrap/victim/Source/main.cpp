#include <stdio.h>
#include <Windows.h>

void main()
{
	for (int i = 0; i < 100; ++i)
	{
		printf("apple: %d\n", i);
		Sleep(1000);
	}
}
