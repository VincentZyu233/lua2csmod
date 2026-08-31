#include <features.h>

#if !defined(__GLIBC__)
#error "Lua2CS Linux builds currently require glibc."
#endif

extern double lua2cs_glibc_fmod(double x, double y);
__asm__(".symver lua2cs_glibc_fmod,fmod@GLIBC_2.2.5");

double __wrap_fmod(double x, double y)
{
    return lua2cs_glibc_fmod(x, y);
}
