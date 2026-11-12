#include <stdio.h>
#include <stdint.h>
#include "soem/soem.h"

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT
#endif

static ecx_contextt ctx;
static int is_open;

EXPORT void shim_close(void)
{
		if(is_open) ecx_close(&ctx);
		is_open = 0;
}

EXPORT int shim_list_adapters(char *buf, int len)
{
	int used = 0;
	ec_adaptert *list = ec_find_adapters();
	for(ec_adaptert *a = list; a != NULL; a = a -> next) {
		int n = snprintf(buf + used, len - used, "%s|%s\n", a->name, a->desc);
		if(n<0 || n>=len - used) { used = -1; break;}
		used += n;
	}
	ec_free_adapters(list);
	return used;
}

EXPORT int shim_open(const char *ifname){
	shim_close();
	if(ecx_init(&ctx, ifname) <= 0) return -1;
	is_open = 1;
	int slaves = ecx_config_init(&ctx);
	if(slaves < 1) return slaves;
	ctx.slavelist[1].state = EC_STATE_PRE_OP;
	ecx_writestate(&ctx, 1);
	if(ecx_statecheck(&ctx, 1, EC_STATE_PRE_OP, EC_TIMEOUTSTATE) != EC_STATE_PRE_OP) return -2;
	return slaves;
}

EXPORT void shim_identity(uint32_t *vendor, uint32_t *product, char *name, int len){
	*vendor = ctx.slavelist[1].eep_man;
	*product = ctx.slavelist[1].eep_id;
	snprintf(name, len, "%s", ctx.slavelist[1].name);
}

EXPORT int shim_state(void){
	return is_open ? ecx_statecheck(&ctx, 1, EC_STATE_PRE_OP, 20000) : 0;
}
EXPORT int shim_sdo_read(uint16_t index, uint8_t sub, void *buf, int *size){
	return is_open ? ecx_SDOread(&ctx, 1, index, sub, FALSE, size, buf, EC_TIMEOUTRXM) : 0;
}

EXPORT int shim_sdo_write(uint16_t index, uint8_t sub, const void *buf, int size){
	return is_open ? ecx_SDOwrite(&ctx, 1, index, sub, FALSE, size, buf, EC_TIMEOUTRXM) : 0;
}